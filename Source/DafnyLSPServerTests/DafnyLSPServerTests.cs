﻿using Xunit;
using Microsoft.Dafny.LSPServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.IO.Pipelines;
using OmniSharp.Extensions.LanguageServer.Protocol.General;
using System.Windows;
using Microsoft.Boogie;
using System.Reflection;
using System.IO;

namespace Microsoft.Dafny.LSPServer.Tests
{
  public class DafnyLSPServerTests
  {
    [Fact()]
    public async void StartTest()
    {
      DafnyOptions.Install(new Dafny.DafnyOptions(null));
      CommandLineOptions.Clo.Z3ExecutablePath = "C:\\Users\\Steen\\source\\repos\\dafny\\Binaries\\z3.exe";
      CommandLineOptions.Clo.ApplyDefaultOptions();

      var clientToServerPipe = new Pipe();
      var serverToClientPipe = new Pipe();

      var serverTask = DafnyLSPServer.Start(clientToServerPipe.Reader, serverToClientPipe.Writer);
            
      var someUri = DocumentUri.File("containsAnInvalidAssertion");
      var content = "method selftest() { assert false; }";
      LanguageClientOptions clientOptions = new LanguageClientOptions();
      clientOptions.WithInput(serverToClientPipe.Reader);
      clientOptions.WithOutput(clientToServerPipe.Writer);
      var client = await LanguageClient.From(clientOptions);
      await serverTask;
      await client.RequestLanguageProtocolInitialize(new InitializeParams
      {
      });

      client.DidOpenTextDocument(new DidOpenTextDocumentParams
      {
          TextDocument = new TextDocumentItem
          {
              Uri = someUri,
              Text = content
          }
      });
      client.DidChangeTextDocument(new DidChangeTextDocumentParams
      {
          TextDocument = new VersionedTextDocumentIdentifier
          {
              Uri = someUri
          },
          ContentChanges = new List<TextDocumentContentChangeEvent>()
      });

      var diagnosticsReceivedSource = new TaskCompletionSource<PublishDiagnosticsParams>();
      var diagnosticsReceived = diagnosticsReceivedSource.Task;
      client.Register(clientRegistry =>
      {
          PublishDiagnosticsExtensions.OnPublishDiagnostics(clientRegistry, diagnosticsParams =>
          {
              diagnosticsReceivedSource.SetResult(diagnosticsParams);
          });
      });

      var diagnosticsparams = await diagnosticsReceived;
      Assert.True(diagnosticsparams.Diagnostics.Count() == 1);
      Assert.Contains("assertion violation", diagnosticsparams.Diagnostics.First().Message);
    }
  }
}