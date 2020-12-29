using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SocialNetworkServerless.AzureSocialNetworkServerless.Models
{
    public class PersonalTimeline :BaseTimeline
    {
        public string ToUserId { get; set; }
        public ApplicationUser ToUser { get; set; }
       
    }
}
