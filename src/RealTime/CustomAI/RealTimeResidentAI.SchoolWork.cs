﻿// <copyright file="RealTimeResidentAI.SchoolWork.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.CustomAI
{
    using RealTime.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private bool ScheduleWork(ref CitizenSchedule schedule, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (!workBehavior.ScheduleGoToWork(ref schedule, currentBuilding, simulationCycle))
            {
                return false;
            }

            Log.Debug($"  - Schedule work at {schedule.ScheduledStateTime}");

            float timeLeft = (float)(schedule.ScheduledStateTime - TimeInfo.Now).TotalHours;
            if (timeLeft <= PrepareToWorkHours)
            {
                // Just sit at home if the work time will come soon
                Log.Debug($"  - Worktime in {timeLeft} hours, doing nothing");
                return true;
            }

            if (timeLeft <= MaxTravelTime)
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    Log.Debug($"  - Worktime in {timeLeft} hours, returning home");
                    schedule.Schedule(ResidentState.AtHome, default);
                    return true;
                }

                // If we have some time, try to shop locally.
                if (ScheduleShopping(ref schedule, ref citizen, true))
                {
                    Log.Debug($"  - Worktime in {timeLeft} hours, trying local shop");
                }
                else
                {
                    Log.Debug($"  - Worktime in {timeLeft} hours, doing nothing");
                }

                return true;
            }

            return false;
        }

        private void DoScheduledWork(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            schedule.WorkStatus = WorkStatus.Working;

            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, schedule.WorkBuilding)
                && schedule.CurrentState == ResidentState.AtHome)
            {
                schedule.DepartureToWorkTime = TimeInfo.Now;
            }

            Log.Debug(TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is going from {currentBuilding} to school/work {schedule.WorkBuilding}");

            Citizen.AgeGroup citizenAge = CitizenProxy.GetAge(ref citizen);
            if (!workBehavior.ScheduleLunch(ref schedule, citizenAge))
            {
                workBehavior.ScheduleReturnFromWork(ref schedule, citizenAge);
            }
        }

        private void DoScheduledLunch(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            ushort lunchPlace = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance);
            if (lunchPlace != 0)
            {
                Log.Debug(TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is going for lunch from {currentBuilding} to {lunchPlace}");
                workBehavior.ScheduleReturnFromLunch(ref schedule);
            }
            else
            {
                Log.Debug(TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go for lunch from {currentBuilding}, but there were no buildings close enough");
                workBehavior.ScheduleReturnFromWork(ref schedule, CitizenProxy.GetAge(ref citizen));
            }
        }
    }
}
