﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Set guest identity", Category = "Session", IsActiveByDefault = true)]
    public class SetGuestIdentityMacro : IMacro
    {
        private static string GUEST_IDENTITY_VARIABLE = "guestIdentity";

        public Task ProcessAsync(EnvelopeViewModel envelopeViewModel, SessionViewModel sessionViewModel)
        {
            if (envelopeViewModel == null) throw new ArgumentNullException(nameof(envelopeViewModel));            
            if (sessionViewModel == null) throw new ArgumentNullException(nameof(sessionViewModel));
            
            var session = envelopeViewModel.Envelope as Session;

            if (session != null &&
                !session.Id.IsNullOrEmpty() &&
                session.State == SessionState.Authenticating)
            {
                var sessionIdVariableViewModel = sessionViewModel
                    .Variables
                    .FirstOrDefault(v => v.Name.Equals(GUEST_IDENTITY_VARIABLE));

                if (sessionIdVariableViewModel == null)
                {
                    sessionIdVariableViewModel = new VariableViewModel()
                    {
                        Name = GUEST_IDENTITY_VARIABLE
                    };

                    sessionViewModel.Variables.Add(sessionIdVariableViewModel);
                }

                var guestNode = new Node
                {
                    Domain = session.From.Domain,
                    Name = EnvelopeId.NewId()
                };

                sessionIdVariableViewModel.Value = guestNode.ToString();
            }

            return Task.FromResult<object>(null);
        }
    }
}
