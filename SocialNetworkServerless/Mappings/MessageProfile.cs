using AutoMapper;
using SocialNetworkServerless.AzureSocialNetworkServerless.Models;
using SocialNetworkServerless.AzureSocialNetworkServerless.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialNetworkServerless.AzureSocialNetworkServerless.Mappings
{
    public class MessageProfile : Profile
    {
        public MessageProfile()
        {
            CreateMap<Tuple<Message, PersonalTimeline>, MessageViewModel>()
                .ForMember(dst => dst.Content, opt => opt.MapFrom(x => x.Item1.Content))
                .ForMember(dst => dst.Timestamp, opt => opt.MapFrom(x => x.Item1.Timestamp))
            .ForMember(dst => dst.From, opt => opt.MapFrom(x => x.Item2.FromUser.FullName))
                .ForMember(dst => dst.ToUser, opt => opt.MapFrom(x => x.Item2.ToUser.UserName))
                .ForMember(dst => dst.To, opt => opt.MapFrom(x => x.Item2.ToRoom.Name));

            CreateMap< MessageViewModel, Tuple<Message, PersonalTimeline>>();
        }
    }
}
