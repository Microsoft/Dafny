﻿using Microsoft.Dafny.LanguageServer.Language;
using Microsoft.Dafny.LanguageServer.Language.Symbols;
using Microsoft.Dafny.LanguageServer.Workspace;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Dafny.LanguageServer.Handlers {
  /// <summary>
  /// LSP Synchronization handler for symbol based events, i.e. the client requests the symbols of the specified document.
  /// </summary>
  public class DafnyDocumentSymbolHandler : DocumentSymbolHandler {
    private static readonly SymbolInformationOrDocumentSymbol[] _emptySymbols = new SymbolInformationOrDocumentSymbol[0];

    private readonly ILogger _logger;
    private readonly IDocumentDatabase _documents;

    public DafnyDocumentSymbolHandler(ILogger<DafnyDocumentSymbolHandler> logger, IDocumentDatabase documents) : base(CreateRegistrationOptions()) {
      _logger = logger;
      _documents = documents;
    }

    private static DocumentSymbolRegistrationOptions CreateRegistrationOptions() {
      return new DocumentSymbolRegistrationOptions {
        DocumentSelector = DocumentSelector.ForLanguage("dafny")
      };
    }

    public override Task<SymbolInformationOrDocumentSymbolContainer> Handle(DocumentSymbolParams request, CancellationToken cancellationToken) {
      DafnyDocument? document;
      if(!_documents.TryGetDocument(request.TextDocument, out document)) {
        _logger.LogWarning("symbols requested for unloaded document {}", request.TextDocument.Uri);
        return Task.FromResult<SymbolInformationOrDocumentSymbolContainer>(_emptySymbols);
      }
      var visitor = new LspSymbolGeneratingVisitor(document.SymbolTable, cancellationToken);
      var symbols = visitor.Visit(document.SymbolTable.CompilationUnit).ToArray();
      return Task.FromResult<SymbolInformationOrDocumentSymbolContainer>(symbols);
    }
  }
}
