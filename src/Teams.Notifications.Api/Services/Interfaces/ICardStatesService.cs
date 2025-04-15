using System.Threading.Tasks;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Services.Interfaces;

public interface ICardStatesService
{
    Task<CardState> GetOrUpdate(CardState currentState);
}