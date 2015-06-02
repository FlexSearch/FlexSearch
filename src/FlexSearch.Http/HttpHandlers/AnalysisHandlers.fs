// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core.HttpHandlers

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Core.HttpHelpers
open System.Collections.Generic

/// <summary>
///  Get an analyzer by Id
/// </summary>
/// <method>GET</method>
/// <uri>/analyzers/:id</uri>
/// <resource>analyzer</resource>
/// <id>get-analyzer-by-id</id>
[<Name("GET-/analyzers/:id")>]
[<Sealed>]
type GetAnalyzerByIdHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<unit, Analyzer>()
    override this.Process(id, subId, body, context) = (analyzerService.GetAnalyzerInfo(id.Value), Ok, NotFound)

/// <summary>
///  Get all analyzer
/// </summary>
/// <method>GET</method>
/// <uri>/analyzers</uri>
/// <resource>analyzer</resource>
/// <id>get-all-analyzer</id>
[<Name("GET-/analyzers")>]
[<Sealed>]
type GetAllAnalyzerHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<unit, List<Analyzer>>()
    override this.Process(id, subId, body, context) = (analyzerService.GetAllAnalyzers(), Ok, BadRequest)

/// <summary>
///  Analyze a text string using the passed analyzer.
/// </summary>
/// <method>POST</method>
/// <uri>/analyzers/:analyzerName/analyze</uri>
/// <resource>analyzer</resource>
/// <id>get-analyze-text</id>
[<Name("POST-/analyzers/:id/analyze")>]
[<Sealed>]
type AnalyzeTextHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<AnalysisRequest, string>()
    override this.Process(id, subId, body, context) = 
        (analyzerService.Analyze(id.Value, body.Value.Text), Ok, BadRequest)

/// <summary>
///  Delete an analyzer by Id
/// </summary>
/// <method>DELETE</method>
/// <uri>/analyzers/:id</uri>
/// <resource>analyzer</resource>
/// <id>delete-analyzer-by-id</id>
[<Name("DELETE-/analyzers/:id")>]
[<Sealed>]
type DeleteAnalyzerByIdHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<unit, unit>()
    override this.Process(id, subId, body, context) = (analyzerService.DeleteAnalyzer(id.Value), Ok, BadRequest)

/// <summary>
///  Create or update an analyzer
/// </summary>
/// <method>PUT</method>
/// <uri>/analyzers/:id</uri>
/// <resource>analyzer</resource>
/// <id>put-analyzer-by-id</id>
[<Name("PUT-/analyzers/:id")>]
[<Sealed>]
type CreateOrUpdateAnalyzerByIdHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<Analyzer, unit>()
    override this.Process(id, subId, body, context) = 
        body.Value.AnalyzerName <- id.Value
        (analyzerService.AddOrUpdateAnalyzer(body.Value), Ok, BadRequest)
