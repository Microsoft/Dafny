﻿using DafnyLS.Language.Symbols;
using Microsoft.Dafny;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DafnyLS.Language {
  /// <summary>
  /// Internal representation of a dafny document.
  /// </summary>
  public class DafnyDocument {
    public TextDocumentItem Text { get; }
    public DocumentUri Uri => Text.Uri;
    public long Version => Text.Version;

    public Microsoft.Dafny.Program Program { get; }
    public ErrorReporter Errors { get; }
    public SymbolTable SymbolTable { get; }

    public DafnyDocument(
        TextDocumentItem textDocument, ErrorReporter errors, Microsoft.Dafny.Program program, SymbolTable symbolTable
    ) {
      Text = textDocument;
      Program = program;
      Errors = errors;
      SymbolTable = symbolTable;
    }
  }
}
