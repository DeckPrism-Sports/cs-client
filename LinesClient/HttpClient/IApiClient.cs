
using DPSports.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DPSports.Feed
{
    public interface IApiClient
    {
        Task<List<Game>> GetGames(DateTime? gamesStart, DateTime? gamesEnd);
    }
}
