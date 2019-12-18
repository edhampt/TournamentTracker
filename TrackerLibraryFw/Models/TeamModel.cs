using System;
using System.Collections.Generic;

namespace TrackerLibrary.Models
{
    public class TeamModel : BaseModel
    {
        public string TeamName { get; set; }
        public List<PersonModel> TeamMembers { get; set; } = new List<PersonModel>();
    }
}
