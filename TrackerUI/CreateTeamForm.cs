using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrackerLibrary;
using TrackerLibrary.Models;

namespace TrackerUI
{
    public partial class CreateTeamForm : Form
    {
        private List<PersonModel> availableTeamMembers = GlobalConfig.Connection.GetPerson_All();
        private List<PersonModel> selectedTeamMembers = new List<PersonModel>();
        private ITeamRequester callingForm;

        public CreateTeamForm(ITeamRequester caller)
        {
            InitializeComponent();

            callingForm = caller;
            //CreateSampleData();
            
            WireUpLists();

        }

        
        private void CreateSampleData()
        {
            availableTeamMembers.Add(new PersonModel("Tim", "Corey", "", ""));
            availableTeamMembers.Add(new PersonModel("Sue", "Storm", "", ""));

            selectedTeamMembers.Add(new PersonModel("Jane", "Smith", "", ""));
            selectedTeamMembers.Add(new PersonModel("Bill", "Jones", "", ""));
        }

        private void WireUpLists()
        {
            selectTeamMemberDropdown.DataSource = null;

            selectTeamMemberDropdown.DataSource = availableTeamMembers;
            selectTeamMemberDropdown.DisplayMember = "FullName";

            teamMembersListbox.DataSource = null;

            teamMembersListbox.DataSource = selectedTeamMembers;
            teamMembersListbox.DisplayMember = "FullName";
        }

        private void createMemberButton_Click(object sender, EventArgs e)
        {
            if (ValidatePersonForm())
            {
                PersonModel model = new PersonModel(firstNameValue.Text, lastNameValue.Text, emailValue.Text, cellphoneValue.Text);

                GlobalConfig.Connection.CreatePerson(model);

                selectedTeamMembers.Add(model);
                WireUpLists();

                firstNameValue.Text = "";
                lastNameValue.Text = "";
                emailValue.Text = "";
                cellphoneValue.Text = "";

                //MessageBox.Show("Information saved");
            }
            else
            {
                MessageBox.Show("This form has invalid information, all fields are required");
            }
        }

        private bool ValidatePersonForm()
        {
            bool output = true;

            if (firstNameValue.Text.Length == 0)
                output = false;
            if (lastNameValue.Text.Length == 0)
                output = false;
            if (emailValue.Text.Length == 0)
                output = false;
            if (cellphoneValue.Text.Length == 0)
                output = false;

            return output;
        }

        private void addMemberButton_Click(object sender, EventArgs e)
        {
            PersonModel p = (PersonModel)selectTeamMemberDropdown.SelectedItem;

            if (p != null)
            {
                availableTeamMembers.Remove(p);
                selectedTeamMembers.Add(p);

                WireUpLists();
            }
        }

        private void removeSelectedTeamButton_Click(object sender, EventArgs e)
        {
            PersonModel p = (PersonModel)teamMembersListbox.SelectedItem;

            if (p != null)
            {
                selectedTeamMembers.Remove(p);
                availableTeamMembers.Add(p);

                WireUpLists();
            }
        }

        private void createTeamButton_Click(object sender, EventArgs e)
        {
            TeamModel t = new TeamModel();
            t.TeamName = teamNameValue.Text;
            t.TeamMembers = selectedTeamMembers;

            GlobalConfig.Connection.CreateTeam(t);

            callingForm.TeamComplete(t);

            this.Close();
        }
    }
}
