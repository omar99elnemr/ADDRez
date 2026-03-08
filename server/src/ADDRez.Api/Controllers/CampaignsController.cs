using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Common;
using ADDRez.Api.DTOs.Settings;
using ADDRez.Api.Entities;
using ADDRez.Api.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly AppDbContext _db;
    public CampaignsController(AppDbContext db) => _db = db;

    [HttpGet]
    [Permission("campaigns.view")]
    public async Task<IActionResult> Index([FromQuery] PaginationQuery q)
    {
        var query = _db.Campaigns.AsQueryable();

        if (!string.IsNullOrEmpty(q.Search))
            query = query.Where(c => c.Name.Contains(q.Search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((q.Page - 1) * q.PerPage).Take(q.PerPage)
            .Select(c => new CampaignListDto(
                c.Id, c.Name, c.Subject, c.Channel, c.Status, c.TargetAudience,
                c.TotalRecipients, c.SentCount, c.OpenCount,
                c.ScheduledAt, c.SentAt, c.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PaginatedResponse<CampaignListDto>(items, q.Page, (int)Math.Ceiling((double)total / q.PerPage), total, q.PerPage));
    }

    [HttpGet("{id}")]
    [Permission("campaigns.view")]
    public async Task<IActionResult> Show(int id)
    {
        var c = await _db.Campaigns.FindAsync(id);
        if (c == null) return NotFound(new { message = "Campaign not found" });
        return Ok(c);
    }

    [HttpPost]
    [Permission("campaigns.create")]
    public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var campaign = new Campaign
        {
            CompanyId = companyId, Name = request.Name, Subject = request.Subject,
            Body = request.Body, Channel = request.Channel, TargetAudience = request.TargetAudience,
            TargetCategoryId = request.TargetCategoryId, TargetTagId = request.TargetTagId,
            ScheduledAt = request.ScheduledAt, CreatedByUserId = userId,
            Status = CampaignStatus.Draft
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();
        return Ok(new { id = campaign.Id });
    }

    [HttpPut("{id}")]
    [Permission("campaigns.edit")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCampaignRequest request)
    {
        var c = await _db.Campaigns.FindAsync(id);
        if (c == null) return NotFound(new { message = "Campaign not found" });
        if (c.Status != CampaignStatus.Draft) return BadRequest(new { message = "Can only edit draft campaigns" });

        c.Name = request.Name; c.Subject = request.Subject; c.Body = request.Body;
        c.Channel = request.Channel; c.TargetAudience = request.TargetAudience;
        c.TargetCategoryId = request.TargetCategoryId; c.TargetTagId = request.TargetTagId;
        c.ScheduledAt = request.ScheduledAt;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Campaign updated" });
    }

    [HttpPost("{id}/send")]
    [Permission("campaigns.send")]
    public async Task<IActionResult> Send(int id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign == null) return NotFound(new { message = "Campaign not found" });
        if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Scheduled)
            return BadRequest(new { message = "Campaign cannot be sent in its current status" });

        // Build recipient list based on target audience
        var customersQuery = _db.Customers.Where(c => c.Status == CustomerStatus.Active);

        if (campaign.TargetAudience == "category" && campaign.TargetCategoryId.HasValue)
            customersQuery = customersQuery.Where(c => c.ClientCategoryId == campaign.TargetCategoryId);
        else if (campaign.TargetAudience == "tag" && campaign.TargetTagId.HasValue)
            customersQuery = customersQuery.Where(c => c.Tags.Any(t => t.Id == campaign.TargetTagId));

        var customerIds = await customersQuery.Select(c => c.Id).ToListAsync();

        foreach (var customerId in customerIds)
        {
            _db.CampaignRecipients.Add(new CampaignRecipient
            {
                CampaignId = id, CustomerId = customerId, Status = "pending"
            });
        }

        campaign.Status = CampaignStatus.Sending;
        campaign.TotalRecipients = customerIds.Count;
        campaign.SentAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // In production, this would queue a background job to send emails/SMS
        // For now, mark as sent immediately
        campaign.Status = CampaignStatus.Sent;
        campaign.SentCount = customerIds.Count;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Campaign sent to {customerIds.Count} recipients" });
    }

    [HttpDelete("{id}")]
    [Permission("campaigns.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Campaigns.FindAsync(id);
        if (c == null) return NotFound(new { message = "Campaign not found" });
        _db.Campaigns.Remove(c);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Campaign deleted" });
    }
}
