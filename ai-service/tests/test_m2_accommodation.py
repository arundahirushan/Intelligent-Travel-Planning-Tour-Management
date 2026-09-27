import pytest
from datetime import datetime
from agents.m2_accommodation import get_hotel_allocations

def test_mixed_room_allocation_exact_match():
    # 5 person group, 1 double and 1 triple available
    rooms = [
        {"RoomId": 101, "RoomType": "Double", "PricePerNight": 8000.0, "Capacity": 2, "AvailableRoomCount": 1},
        {"RoomId": 102, "RoomType": "Triple", "PricePerNight": 11000.0, "Capacity": 3, "AvailableRoomCount": 1},
        {"RoomId": 103, "RoomType": "Single", "PricePerNight": 5000.0, "Capacity": 1, "AvailableRoomCount": 1}
    ]
    group_size = 5
    
    allocations = get_hotel_allocations(rooms, group_size)
    assert len(allocations) > 0
    
    best_alloc = allocations[0]
    
    # Expect 1 Double and 1 Triple
    assert best_alloc["allocation"] == {101: 1, 102: 1}
    assert best_alloc["capacity"] == 5
    assert best_alloc["cost"] == 19000.0
    
def test_mixed_room_allocation_insufficient():
    # 5 person group, only 1 double available
    rooms = [
        {"RoomId": 101, "RoomType": "Double", "PricePerNight": 8000.0, "Capacity": 2, "AvailableRoomCount": 1}
    ]
    group_size = 5
    
    allocations = get_hotel_allocations(rooms, group_size)
    assert len(allocations) == 0

def test_mixed_room_allocation_split_hotel():
    # M2's logic groups rooms by hotel before calling get_hotel_allocations.
    # We verify that given rooms for ONE hotel, if capacity is insufficient, it fails.
    # It cannot reach into another hotel's room list because it's never passed them together.
    hotel1_rooms = [
        {"RoomId": 101, "RoomType": "Double", "PricePerNight": 8000.0, "Capacity": 2, "AvailableRoomCount": 1}
    ]
    hotel2_rooms = [
        {"RoomId": 201, "RoomType": "Triple", "PricePerNight": 11000.0, "Capacity": 3, "AvailableRoomCount": 1}
    ]
    group_size = 5
    
    # Attempting to allocate per hotel fails
    assert len(get_hotel_allocations(hotel1_rooms, group_size)) == 0
    assert len(get_hotel_allocations(hotel2_rooms, group_size)) == 0
