using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SocialNetworkServerless.AzureSocialNetworkServerless.Models
{
    public class BaseTimeline
    {
        public int Id { get; set; }
        public string FromUserId { get; set; }
        [ForeignKey("FromUserId")]
        public ApplicationUser FromUser { get; set; }
        public int ToRoomId { get; set; }
        public Room ToRoom { get; set; }
        public int MessageId { get; set; }
        public Message Message { get; set; }

    }
}
