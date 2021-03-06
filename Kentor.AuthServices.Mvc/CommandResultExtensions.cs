﻿using Kentor.AuthServices.HttpModule;
using Kentor.AuthServices.WebSso;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Kentor.AuthServices.Mvc
{
    /// <summary>
    /// Extension methods for CommandResult for integrating CommandResults in
    /// the MVC architecture.
    /// </summary>
    public static class CommandResultExtensions
    {
        /// <summary>
        /// Converts a command result to an action result.
        /// </summary>
        /// <param name="commandResult">The source command result.</param>
        /// <param name="response">Http Response to write the result to.</param>
        /// <returns>
        /// Action result
        /// </returns>
        /// <exception cref="System.ArgumentNullException">commandResult</exception>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <remarks>
        /// The reason to use a separate command result at all, instead
        /// of simply using ActionResult is that the core library should not
        /// be Mvc dependant.
        /// </remarks>
        public static ActionResult ToActionResult(this CommandResult commandResult, HttpResponseBase response)
        {
            if (commandResult == null)
            {
                throw new ArgumentNullException("commandResult");
            }

            response.SetStoredRequestState(commandResult.StoredRequestState);

            switch (commandResult.HttpStatusCode)
            {
                case HttpStatusCode.SeeOther:
                    return new RedirectResult(commandResult.Location.OriginalString);
                case HttpStatusCode.OK:
                    var result = new ContentResult()
                    {
                        Content = commandResult.Content
                    };

                    if(!string.IsNullOrEmpty(commandResult.ContentType))
                    {
                        result.ContentType = commandResult.ContentType;
                    }

                    return result;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Establishes an application session by calling the session authentication module.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static void SignInSessionAuthenticationModule(this CommandResult commandResult)
        {
            if (commandResult == null)
            {
                throw new ArgumentNullException("commandResult");
            }
            // Ignore this if we're not running inside IIS, e.g. in unit tests.
            if (commandResult.Principal != null && HttpContext.Current != null)
            {
                var sessionToken = new SessionSecurityToken(commandResult.Principal);

                FederatedAuthentication.SessionAuthenticationModule
                    .AuthenticateSessionSecurityToken(sessionToken, true);
            }
        }
    }
}
