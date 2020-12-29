using SocialNetworkServerless.AzureSocialNetworkServerless.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialNetworkServerless.AzureSocialNetworkServerless.Data.Configurations
{
    public class BaseTimelineConfiguration : IEntityTypeConfiguration<BaseTimeline>
    {
        public void Configure(EntityTypeBuilder<BaseTimeline> builder)
        {
            builder.ToTable("BaseTimeline");

            builder.HasOne(s => s.ToRoom)
                .WithMany(m => m.BaseTimelines)
                .HasForeignKey(s => s.ToRoomId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(s => s.Message)
                .WithMany(m => m.BaseTimelines)
                .HasForeignKey(s => s.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
