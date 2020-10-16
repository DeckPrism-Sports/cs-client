using System;
using System.Collections.Generic;

namespace DPSports.Entity.DTO
{
    /// <summary>
    /// The type of market: Money Line = 0, Handicap = 1, Total = 2
    /// </summary>
    public enum MarketType : int
    {
        /// <summary>
        /// Money Line
        /// </summary>
        MoneyLine = 0,
        /// <summary>
        /// Handicap
        /// </summary>
        Spread = 1,
        /// <summary>
        /// Total
        /// </summary>
        Total = 2
    }

    /// <summary>
    /// Represents a Game with it's lines
    /// </summary>
    public class Game
    {
        /// <summary>
        /// The id of the game
        /// </summary>       
        public long Id { get; set; }
        /// <summary>
        /// The sport to which the game belongs to
        /// </summary>
        public Sports Sport { get; set; }
        /// <summary>
        /// The group within the sport to which the game belongs to
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// The start time of the game in Coordinated Universal Time
        /// </summary>
        public DateTime StartTimeUTC { get; set; }
        /// <summary>
        /// The rotation number of the away team
        /// </summary>
        public int AwayRotation { get; set; }
        /// <summary>
        /// The rotation number of the home team
        /// </summary>
        public int HomeRotation { get; set; }
        /// <summary>
        /// The name of the away team
        /// </summary>
        public string AwayName { get; set; }
        /// <summary>
        /// The name of the home team
        /// </summary>
        public string HomeName { get; set; }
        /// <summary>
        /// True if the lines are active for this game
        /// </summary>
        public bool LinesActive { get; set; }
        /// <summary>
        /// The list of market lines available for this game
        /// </summary>
        public List<Market> Markets { get; set; }
    }

    /// <summary>
    /// Represents one market withing one Line of one event
    /// </summary>
    public class Market
    {     
        /// <summary>
        /// Unique identifier for the market
        /// </summary>
        public string LineId { get; set; }
        /// <summary>
        /// Spread or Total
        /// </summary>
        public double? Handicap { get; set; }
        /// <summary>
        /// The price
        /// </summary>
        public double? Odd { get; set; }
        /// <summary>
        /// True if the market is suspended
        /// </summary>
        public bool Suspended { get; set; }
        /// <summary>
        /// The name of the market
        /// </summary>
        public string MarketName { get; set; }
        /// <summary>
        /// The type of market: Money Line = 0, Handicap = 1, Total = 2
        /// </summary>       
        public MarketType MarketType { get; set; }
        /// <summary>
        /// True if this is a single option market
        /// </summary>
        public bool IsSingle { get; set; }
    }
}
