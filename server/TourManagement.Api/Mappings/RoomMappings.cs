using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert Room entities to/from DTOs.
public static class RoomMappings
{
    public static RoomDto ToDto(this Room room)
    {
        return new RoomDto
        {
            Id           = room.Id,
            RoomType     = room.RoomType,
            PricePerNight = room.PricePerNight,
            Capacity     = room.Capacity,
            TotalRooms   = room.TotalRooms,
            Amenities    = room.Amenities,
            Status       = room.Status
        };
    }

    public static RoomWithHotelDto ToRoomWithHotelDto(this Room room)
    {
        return new RoomWithHotelDto
        {
            Id           = room.Id,
            HotelId      = room.HotelId,
            HotelName    = room.Hotel?.Name ?? string.Empty,
            RoomType     = room.RoomType,
            PricePerNight = room.PricePerNight,
            Capacity     = room.Capacity,
            TotalRooms   = room.TotalRooms,
            Amenities    = room.Amenities,
            Status       = room.Status
        };
    }

    public static Room ToEntity(this CreateRoomDto dto, int hotelId)
    {
        return new Room
        {
            HotelId       = hotelId,
            RoomType      = dto.RoomType,
            PricePerNight = dto.PricePerNight,
            Capacity      = dto.Capacity,
            TotalRooms    = dto.TotalRooms,
            Amenities     = dto.Amenities,
            Status        = RoomStatus.Active,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };
    }

    public static void UpdateFromDto(this Room room, UpdateRoomDto dto)
    {
        room.RoomType      = dto.RoomType;
        room.PricePerNight = dto.PricePerNight;
        room.Capacity      = dto.Capacity;
        room.TotalRooms    = dto.TotalRooms;
        room.Amenities     = dto.Amenities;
        room.UpdatedAt     = DateTime.UtcNow;
    }
}
