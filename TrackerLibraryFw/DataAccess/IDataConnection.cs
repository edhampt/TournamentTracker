using System;
using System.Collections.Generic;
using System.Text;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess
{
    public interface IDataConnection
    {
        void CreatePrize(PrizeModel model);

        void CreatePerson(PersonModel model);

        void CreateTournament(TournamentModel model);

        void CreateTeam(TeamModel model);

        List<PersonModel> GetPerson_All();

        List<TeamModel> GetTeam_All();

        List<TournamentModel> GetTournament_All();

        void UpdateMatchup(MatchupModel model);

        void CompleteTournament(TournamentModel model);
    }
}
