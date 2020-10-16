using DPSports.Entity.DTO;
using System.Collections.Generic;
using System.Linq;

namespace DPSports.Logging
{
    public static class LogUtils
    {
        public static List<string> ComposeMarketsForLogging(List<Market> markets)
        {
            return markets?.Select(x => $"[{x.LineId}] - Suspended:{x.Suspended} HC:{x.Handicap} ODD:{x.Odd}").ToList();
        }
    }
}
