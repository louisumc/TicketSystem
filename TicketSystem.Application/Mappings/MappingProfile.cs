using AutoMapper;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ============================================
            // BUS MAPPINGS
            // ============================================
            CreateMap<Bus, BusResponseDto>()
            .ForMember(dest => dest.TotalTrips, opt => opt.MapFrom(src => src.Trips.Count(t => t.IsActive)));

            CreateMap<CreateBusDto, Bus>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Trips, opt => opt.Ignore());

            CreateMap<UpdateBusDto, Bus>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Trips, opt => opt.Ignore());

            // ============================================
            // TRIP MAPPINGS
            // ============================================
            CreateMap<Trip, TripResponseDto>()
            .ForMember(dest => dest.BusPlate, opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Plate : string.Empty))
            .ForMember(dest => dest.BusModel, opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Model : string.Empty))
            .ForMember(dest => dest.BusCompany, opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Company : string.Empty));

            CreateMap<Trip, TripDetailsDto>()
            .ForMember(dest => dest.BusPlate, opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Plate : string.Empty))
            .ForMember(dest => dest.BusModel, opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Model : string.Empty))
            .ForMember(dest => dest.BusCompany, opt => opt.MapFrom(src => src.Bus != null ? src.Bus.Company : string.Empty))
            .ForMember(dest => dest.Seats, opt => opt.MapFrom(src => src.Seats.Where(s => s.IsActive).OrderBy(s => s.Row).ThenBy(s => s.Column)))
            .ForMember(dest => dest.TotalSeats, opt => opt.MapFrom(src => src.Seats.Count(s => s.IsActive)))
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src => src.Seats.Count(s => s.IsActive && s.Status == Domain.Enums.SeatStatus.Available)))
            .ForMember(dest => dest.ReservedSeats, opt => opt.MapFrom(src => src.Seats.Count(s => s.IsActive && s.Status == Domain.Enums.SeatStatus.Reserved)))
            .ForMember(dest => dest.SoldSeats, opt => opt.MapFrom(src => src.Seats.Count(s => s.IsActive && s.Status == Domain.Enums.SeatStatus.Sold)))
            .ForMember(dest => dest.MaintenanceSeats, opt => opt.MapFrom(src => src.Seats.Count(s => s.IsActive && s.Status == Domain.Enums.SeatStatus.Maintenance)));

            CreateMap<CreateTripDto, Trip>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Bus, opt => opt.Ignore())
            .ForMember(dest => dest.Seats, opt => opt.Ignore());

            CreateMap<UpdateTripDto, Trip>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Bus, opt => opt.Ignore())
            .ForMember(dest => dest.Seats, opt => opt.Ignore());

            // ============================================
            // SEAT MAPPINGS
            // ============================================
            CreateMap<Seat, SeatDto>();

            CreateMap<CreateSeatDto, Seat>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Trip, opt => opt.Ignore());

            CreateMap<UpdateSeatDto, Seat>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Trip, opt => opt.Ignore());
        }
    }
}

