﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TrackerLibrary.Models;
using Dapper;
using System.Linq;

namespace TrackerLibrary.DataAccess
{
    public class SqlConnector : IDataConnection
    {
        private const string ConnString = "Tournaments";
        public void CreatePerson(PersonModel model)
        {
            using (IDbConnection connection = GetConnection())
            {
                var p = new DynamicParameters();
                p.Add("@firstName", model.FirstName);
                p.Add("@lastName", model.LastName);
                p.Add("@emailAddress", model.EmailAddress);
                p.Add("@cellPhoneNumber", model.CellphoneNumber);
                p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spPeople_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@id");

            }
        }

        public void CreatePrize(PrizeModel model)
        {
            using (IDbConnection connection = GetConnection())
            {
                var p = new DynamicParameters();
                p.Add("@placeNumber", model.PlaceNumber);
                p.Add("@placeName", model.PlaceName);
                p.Add("@prizeAmount", model.PrizeAmount);
                p.Add("@prizePercentage", model.PrizePercentage);
                p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spPrizes_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@id");

            }
        }

        public void CreateTeam(TeamModel model)
        {
            using (IDbConnection connection = GetConnection())
            {
                var p = new DynamicParameters();
                p.Add("@teamName", model.TeamName);
                p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTeams_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@id");

                foreach (var tm in model.TeamMembers)
                {
                    p = new DynamicParameters();
                    p.Add("@teamId", model.Id);
                    p.Add("@personId", tm.Id);

                    connection.Execute("dbo.spTeamMembers_Insert", p, commandType: CommandType.StoredProcedure);

                }

            }
        }

        public void CreateTournament(TournamentModel model)
        {
            using (IDbConnection connection = GetConnection())
            {

                SaveTournament(connection, model);

                SaveTournamentPrizes(connection, model);

                SaveTournamentEntries(connection, model);

                SaveTournamentRounds(connection, model);

                TournamentLogic.UpdateTournamentResults(model);
            }
        }

        private void SaveTournament(IDbConnection connection, TournamentModel model)
        {
            var p = new DynamicParameters();
            p.Add("@tournamentName", model.TournamentName);
            p.Add("@entryFee", model.EntryFee);
            p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute("dbo.spTournament_Insert", p, commandType: CommandType.StoredProcedure);

            model.Id = p.Get<int>("@id");
        }

        private void SaveTournamentPrizes(IDbConnection connection, TournamentModel model)
        {
            foreach (var pz in model.Prizes)
            {
                var p = new DynamicParameters();
                p.Add("@tournamentId", model.Id);
                p.Add("@prizeId", pz.Id);
                p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTournamentPrizes_Insert", p, commandType: CommandType.StoredProcedure);
            }
        }

        private void SaveTournamentEntries(IDbConnection connection, TournamentModel model)
        {
            foreach (var tm in model.EnteredTeams)
            {
                var p = new DynamicParameters();
                p.Add("@tournamentId", model.Id);
                p.Add("@teamId", tm.Id);
                p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTournamentEntries_Insert", p, commandType: CommandType.StoredProcedure);
            }
        }

        private void SaveTournamentRounds(IDbConnection connection, TournamentModel model)
        {
            // loop through the rounds
            foreach (List<MatchupModel> round in model.Rounds)
            {
                // loop through the matchups
                foreach (MatchupModel matchup in round)
                {
                    // save the matchup
                    var p = new DynamicParameters();
                    p.Add("@tournamentId", model.Id);
                    p.Add("@matchupRound", matchup.MatchupRound);
                    p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

                    connection.Execute("dbo.spMatchups_Insert", p, commandType: CommandType.StoredProcedure);

                    matchup.Id = p.Get<int>("@id");

                    // loop through the entries and save them
                    foreach (MatchupEntryModel entry in matchup.Entries)
                    {
                        p = new DynamicParameters();
                        p.Add("@matchupId", matchup.Id);
                        p.Add("@parentMatchupId", entry.ParentMatchup?.Id);
                        p.Add("@teamCompetingId", entry.TeamCompeting?.Id);
                        p.Add("@id", 0, DbType.Int32, direction: ParameterDirection.Output);

                        connection.Execute("dbo.spMatchupEntries_Insert", p, commandType: CommandType.StoredProcedure);

                        entry.Id = p.Get<int>("@id");
                    }
                }
            }
        }

        public List<PersonModel> GetPerson_All()
        {
            List<PersonModel> output;

            using (IDbConnection connection = GetConnection())
            {
                output = connection.Query<PersonModel>("dbo.spPeople_GetAll").ToList();
            }

            return output;
        }

        public List<TeamModel> GetTeam_All()
        {
            List<TeamModel> output;

            using (IDbConnection connection = GetConnection())
            {
                output = connection.Query<TeamModel>("dbo.spTeam_GetAll").ToList();

                foreach (TeamModel team in output)
                {
                    var p = new DynamicParameters();
                    p.Add("@TeamId", team.Id);
                    team.TeamMembers = connection.Query<PersonModel>("dbo.spTeamMembers_GetByTeam", p, commandType: CommandType.StoredProcedure).ToList();
                }
            }

            return output;
        }

        private IDbConnection GetConnection()
        {
            return new System.Data.SqlClient.SqlConnection(GlobalConfig.ConnString(ConnString));
        }

        public List<TournamentModel> GetTournament_All()
        {
            List<TournamentModel> output;

            using (IDbConnection connection = GetConnection())
            {
                output = connection.Query<TournamentModel>("dbo.spTournaments_GetAll").ToList();
                List<TeamModel> allTeams = GetTeam_All();

                foreach (TournamentModel t in output)
                {
                    // populate prizes
                    var p = new DynamicParameters();
                    p.Add("@tournamentId", t.Id);
                    t.Prizes = connection.Query<PrizeModel>("dbo.spPrizes_GetByTournament", p, commandType: CommandType.StoredProcedure).ToList();

                    // populate teams
                    p = new DynamicParameters();
                    p.Add("@tournamentId", t.Id); 
                    t.EnteredTeams = connection.Query<TeamModel>("dbo.spTeam_GetByTournament", p, commandType: CommandType.StoredProcedure).ToList();

                    foreach (TeamModel team in t.EnteredTeams)
                    {
                        p = new DynamicParameters();
                        p.Add("@TeamId", team.Id);
                        team.TeamMembers = connection.Query<PersonModel>("dbo.spTeamMembers_GetByTeam", p, commandType: CommandType.StoredProcedure).ToList();
                    }

                    // populate rounds
                    p = new DynamicParameters();
                    p.Add("@tournamentId", t.Id);
                    List<MatchupModel> matchups = connection.Query<MatchupModel>("dbo.spMatchups_GetByTournament", p, commandType: CommandType.StoredProcedure).ToList();

                    foreach (MatchupModel m in matchups)
                    {
                        p = new DynamicParameters();
                        p.Add("@matchupId", m.Id);
                        m.Entries = connection.Query<MatchupEntryModel>("dbo.spMatchupEntries_GetByMatchupId", p, commandType: CommandType.StoredProcedure).ToList();

                        if (m.WinnerId > 0)
                        {
                            m.Winner = allTeams.Where(x => x.Id == m.WinnerId).First();
                        }

                        foreach (MatchupEntryModel me in m.Entries)
                        {
                            if (me.TeamCompetingId > 0)
                            {
                                me.TeamCompeting = allTeams.Where(x => x.Id == me.TeamCompetingId).First();
                            }

                            if (me.ParentMatchupId > 0)
                            {
                                me.ParentMatchup = matchups.Where(x => x.Id == me.ParentMatchupId).First();
                            }
                        }
                    }

                    List<MatchupModel> currentRow = new List<MatchupModel>();
                    int currentRound = 1;
                    foreach (MatchupModel m in matchups)
                    {
                        if (m.MatchupRound > currentRound)
                        {
                            t.Rounds.Add(currentRow);
                            currentRow = new List<MatchupModel>();
                            currentRound++;
                        }

                        currentRow.Add(m);
                    }

                    t.Rounds.Add(currentRow);
                }
            }

            return output;
        }

        public void UpdateMatchup(MatchupModel model)
        {
            using (IDbConnection connection = GetConnection())
            {
                var p = new DynamicParameters();
                if (model.Winner != null)
                {
                    p.Add("@id", model.Id);
                    p.Add("@winnerId", model.Winner.Id);

                    connection.Execute("dbo.spMatchups_Update", p, commandType: CommandType.StoredProcedure); 
                }

                // spMatchupEntries_Update id, teamcompetingid, score
                foreach (MatchupEntryModel me in model.Entries)
                {
                    if (me.TeamCompeting != null)
                    {
                        p = new DynamicParameters();
                        p.Add("@id", me.Id);
                        p.Add("@teamCompetingId", me.TeamCompeting?.Id);
                        p.Add("@score", me.Score);

                        connection.Execute("dbo.spMatchupEntries_Update", p, commandType: CommandType.StoredProcedure); 
                    }
                }
            }
        }

        public void CompleteTournament(TournamentModel model)
        {
            using (IDbConnection connection = GetConnection())
            {
                var p = new DynamicParameters();
                p.Add("@id", model.Id);

                connection.Execute("dbo.spTournaments_Complete", p, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
