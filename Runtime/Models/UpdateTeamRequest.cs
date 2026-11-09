using System.Collections.Generic;

namespace Technyx.Sdk.Models
{
    // The backend treats a missing description or manager as "unchanged" and an explicit null as "clear",
    // so those two are set through methods and a plain null cannot be confused with "not set".
    public sealed class UpdateTeamRequest
    {
        public string Name;

        private bool _descriptionSet;
        private string _description;
        private bool _managerSet;
        private string _managerId;

        public UpdateTeamRequest SetDescription(string description)
        {
            _descriptionSet = true;
            _description = description;
            return this;
        }

        public UpdateTeamRequest ClearDescription() => SetDescription(null);

        public UpdateTeamRequest SetManager(string userId)
        {
            _managerSet = true;
            _managerId = userId;
            return this;
        }

        public UpdateTeamRequest ClearManager() => SetManager(null);

        internal Dictionary<string, object> ToPayload()
        {
            var payload = new Dictionary<string, object>();

            if (Name != null)
                payload["name"] = Name;

            if (_descriptionSet)
                payload["description"] = _description;

            if (_managerSet)
                payload["manager_id"] = _managerId;

            return payload;
        }
    }
}
