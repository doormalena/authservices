﻿using Kentor.AuthServices.Configuration;
using Kentor.AuthServices.WebSso;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Web;

namespace Kentor.AuthServices.HttpModule
{
  /// <summary>
  /// Http Module for SAML2 authentication. The module hijacks the 
  /// ~/Saml2AuthenticationModule/ path of the http application to provide 
  /// authentication services.
  /// </summary>
  // Not included in code coverage as the http module is tightly dependent on IIS.
  [ExcludeFromCodeCoverage]
  public class Saml2AuthenticationModule : IHttpModule
  {
    private IOptions options;

    /// <summary>
    /// Init the module and subscribe to events.
    /// </summary>
    /// <param name="context"></param>
    public void Init(HttpApplication context)
    {
      if (context == null)
      {
        throw new ArgumentNullException("context");
      }
      context.BeginRequest += OnBeginRequest;

      // Cache configuration during the lifecycle of this module including metadata, certificates etc. 
      options = Options.FromConfiguration;
    }

    /// <summary>
    /// Begin request handler that captures all traffic to ~/Saml2AuthenticationModule/
    /// </summary>
    /// <param name="sender">The http application.</param>
    /// <param name="e">Ignored</param>
    protected virtual void OnBeginRequest(object sender, EventArgs e)
    {
      var application = (HttpApplication)sender;

      // Strip the leading ~ from the AppRelative path.
      var appRelativePath = application.Request.AppRelativeCurrentExecutionFilePath;
      appRelativePath = (!String.IsNullOrEmpty(appRelativePath)) ? appRelativePath.Substring(1) : String.Empty;

      var modulePath = options.SPOptions.ModulePath;

      if (appRelativePath.StartsWith(modulePath, StringComparison.OrdinalIgnoreCase))
      {
        var commandName = appRelativePath.Substring(modulePath.Length);

        var command = CommandFactory.GetCommand(commandName);
        var commandResult = RunCommand(application, command, options);

        commandResult.SignInSessionAuthenticationModule();
        commandResult.Apply(new HttpResponseWrapper(application.Response));
      }
    }

    private static CommandResult RunCommand(HttpApplication application, ICommand command, IOptions options)
    {
      try
      {
        return command.Run(
            new HttpRequestWrapper(application.Request).ToHttpRequestData(),
            options);
      }
      catch (AuthServicesException)
      {
        return new CommandResult
        {
          HttpStatusCode = HttpStatusCode.InternalServerError
        };
      }
    }

    /// <summary>
    /// IDisposable implementation.
    /// </summary>
    public virtual void Dispose()
    {
      // Deliberately do nothing, unsubscribing from events is not
      // needed by the IIS model. Trying to do so throws exceptions.
    }
  }
}