using AutoMapper;
using CentCom.Dtos;
using CentCom.Models;

namespace CentCom.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<AuthenticateRequest, User>();
            CreateMap<CharacterRequest, Character>();
            CreateMap<Character[], CharactersResponse>();
        }
    }
}