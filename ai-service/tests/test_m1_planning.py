import pytest
from datetime import datetime
from agents.m1_planning import validate_plan
from schemas.models import TripPlan, DailyVisit, OvernightSection, PlanningSummary

def test_validate_plan_valid():
    start_date = datetime(2026, 1, 1).date()
    end_date = datetime(2026, 1, 3).date()
    selected_destinations = [{"DestinationId": 1}, {"DestinationId": 2}, {"DestinationId": 3}]
    
    plan = TripPlan(
        daily_visits=[
            DailyVisit(day_number=1, date="2026-01-01", visited_area_ids=[1], visited_area_names=["A"], overnight_area_id=1, overnight_area_name="A", explanation=""),
            DailyVisit(day_number=2, date="2026-01-02", visited_area_ids=[2, 3], visited_area_names=["B", "C"], overnight_area_id=3, overnight_area_name="C", explanation=""),
            DailyVisit(day_number=3, date="2026-01-03", visited_area_ids=[3], visited_area_names=["C"], overnight_area_id=None, overnight_area_name=None, explanation="")
        ],
        overnight_sections=[
            OvernightSection(overnight_area_id=1, overnight_area_name="A", check_in_date="2026-01-01", check_out_date="2026-01-02", night_count=1),
            OvernightSection(overnight_area_id=3, overnight_area_name="C", check_in_date="2026-01-02", check_out_date="2026-01-03", night_count=1)
        ],
        planning_summary=PlanningSummary(explanation="Ok", warnings_or_limitations=[])
    )
    
    errors = validate_plan(plan, start_date, end_date, selected_destinations)
    assert not errors

def test_validate_plan_one_day():
    start_date = datetime(2026, 1, 1).date()
    end_date = datetime(2026, 1, 1).date()
    selected_destinations = [{"DestinationId": 1}]
    
    plan = TripPlan(
        daily_visits=[
            DailyVisit(day_number=1, date="2026-01-01", visited_area_ids=[1], visited_area_names=["A"], overnight_area_id=None, overnight_area_name=None, explanation="")
        ],
        overnight_sections=[],
        planning_summary=PlanningSummary(explanation="Ok", warnings_or_limitations=[])
    )
    
    errors = validate_plan(plan, start_date, end_date, selected_destinations)
    assert not errors

def test_validate_plan_missing_area():
    start_date = datetime(2026, 1, 1).date()
    end_date = datetime(2026, 1, 2).date()
    selected_destinations = [{"DestinationId": 1}, {"DestinationId": 2}]
    
    plan = TripPlan(
        daily_visits=[
            DailyVisit(day_number=1, date="2026-01-01", visited_area_ids=[1], visited_area_names=["A"], overnight_area_id=1, overnight_area_name="A", explanation=""),
            DailyVisit(day_number=2, date="2026-01-02", visited_area_ids=[1], visited_area_names=["A"], overnight_area_id=None, overnight_area_name=None, explanation="")
        ],
        overnight_sections=[
            OvernightSection(overnight_area_id=1, overnight_area_name="A", check_in_date="2026-01-01", check_out_date="2026-01-02", night_count=1)
        ],
        planning_summary=PlanningSummary(explanation="Ok", warnings_or_limitations=[])
    )
    
    errors = validate_plan(plan, start_date, end_date, selected_destinations)
    assert any("Plan misses selected areas" in e for e in errors)
