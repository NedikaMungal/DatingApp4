using System;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MemberRepository(AppDbContext context) : IMemberRepository
{
    public async Task<Member?> GetMemberByIdAsync(string id)
    {
        return await context.Members.FindAsync(id);
    }

    public async Task<Member?> GetMemberForUpdateAsync(string id)
    {
        return await context.Members
                        .Include(x => x.User)
                        .Include(x => x.Photos)
                        .IgnoreQueryFilters()
                        .SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PaginatedResult<Member>> GetMembersAsync(MemberParams memberParams)
    {

        var query = context.Members.AsQueryable();
        query = query.Where(x => x.Id != memberParams.CurrentMemberId);

        if (memberParams.Gender != null)
        {
            query = query.Where(x => x.Gender == memberParams.Gender);
        }

        var minDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-memberParams.MaxAge - 1));
        var maxDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-memberParams.MinAge));

        query = query.Where(x => x.DateOfBirth >= minDob && x.DateOfBirth <= maxDob);

        query = memberParams.OrderBy switch
        {
            "created" => query.OrderByDescending(x => x.Created),
            _ => query.OrderByDescending(x => x.LastActive)
        };

        return await PaginationHelper.CreateAsync(query, memberParams.PageNumber, memberParams.PageSize);
    }
    public async Task<IReadOnlyList<MessageDto>> GetMessageThread(string currentMemberId, string recipientId)
    {
        await context.Messages
            .Where(x => x.RecipientId == currentMemberId
                && x.SenderId == recipientId && x.DateRead == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DateRead, DateTime.UtcNow));

        return await context.Messages
            .Where(x => (x.RecipientId == currentMemberId && x.SenderId == recipientId)
                || (x.SenderId == currentMemberId && x.RecipientId == recipientId))
            .OrderBy(x => x.MessageSent)
            .Select(MessageExtensions.ToDtoProjection())
            .ToListAsync();
    }
    public async Task<IReadOnlyList<Photo>> GetPhotosForMemberAsync(string memberId)
    {
        return await context.Members
            .Where(x => x.Id == memberId)
            .SelectMany(x => x.Photos)
            .ToListAsync();
    }
    public void Update(Member member)
    {
        context.Entry(member).State = EntityState.Modified;
    }

    public async Task<IEnumerable<Photo>> GetPhotosForMemberAsync(string userId, bool isCurrentUser)
    {
        var query = context.Members
                        .Where(x => x.Id == userId)
                        .SelectMany(x => x.Photos);

        if (isCurrentUser) query = query.IgnoreQueryFilters();
        return await query.ToListAsync();
    }
}