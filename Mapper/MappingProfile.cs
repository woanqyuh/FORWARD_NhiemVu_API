using AutoMapper;
using ForwardMessage.Models;
using ForwardMessage.Dtos;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserDto, UserModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))  
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy.ToString()));

        CreateMap<ChatGroupDto, ChatGroup>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy.ToString()))
            .ForMember(dest => dest.WorkStartTime, opt => opt.MapFrom(src => src.WorkStartTime.ToString(@"hh\:mm")))
            .ForMember(dest => dest.WorkEndTime, opt => opt.MapFrom(src => src.WorkEndTime.ToString(@"hh\:mm")));

        CreateMap<TaskDto, TaskModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy.ToString()));
        CreateMap<KeyDto, KeyModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy.ToString()));
    }
}