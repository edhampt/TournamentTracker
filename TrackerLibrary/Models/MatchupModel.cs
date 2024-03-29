﻿using System.Collections.Generic;

namespace TrackerLibrary.Models
{
    public class MatchupModel : BaseModel
    {
        public List<MatchupEntryModel> Entries { get; set; } = new List<MatchupEntryModel>();
        public TeamModel Winner { get; set; }
        public int MatchupRound { get; set; }
    }
}