using AutoMapper;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Bus -> DTO
            CreateMap<Bus, BusResponseDto>()
                .ForMember(dest => dest.TotalTrips,
                    opt => opt.MapFrom(src => src.Trips.Count(t => t.IsActive)));

            // DTO -> Bus
            CreateMap<CreateBusDto, Bus>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Trips, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            CreateMap<UpdateBusDto, Bus>()
                .ForMember(dest => dest.Trips, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Trip -> DTO
            CreateMap<Trip, TripResponseDto>()
                .ForMember(dest => dest.BusPlate,
                    opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Plate : string.Empty))
                .ForMember(dest => dest.BusModel,
                    opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Model : string.Empty))
                .ForMember(dest => dest.BusCompany,
                    opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Company : string.Empty));

            // DTO -> Trip
            CreateMap<CreateTripDto, Trip>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Bus, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            CreateMap<UpdateTripDto, Trip>()
                .ForMember(dest => dest.Bus, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }
    }
}