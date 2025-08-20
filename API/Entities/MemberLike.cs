
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace API.Entities;

[Table("Likes")]
public class MemberLike
{
    public required string SourceMemberId { get; set; }
    public Member SourceMember { get; set; } = null!;

    public required string TargetMemberId { get; set; }
    public Member TargetMember { get; set; } = null!;
}