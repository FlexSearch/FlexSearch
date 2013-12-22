//namespace FlexSearch.Server.Services
//{
//    using System.Net;

//    using FlexSearch.Api.Analysis;
//    using FlexSearch.Core;

//    using org.apache.lucene.queryparser.classic;

//    using ServiceStack.Common.Web;
//    using ServiceStack.ServiceInterface;

//    public class AnalysisService : Service
//    {
//        public Interface.IFactoryCollection FactoryCollection { get; set; }

//        public AnalyzeResponse Any(Analyze request)
//        {
//            var analyzer = FactoryCollection.AnalyzerFactory.GetModuleByName(request.AnalyzerName);
//            if (analyzer == null)
//            {
//                throw new HttpError(HttpStatusCode.BadRequest, "AnalyzerNotExists", "Requested analyzer does not exist.");
//            }

//            var queryParser = new QueryParser(Constants.LuceneVersion, "", analyzer.Value);
//            return new AnalyzeResponse { AnalyzedText = queryParser.parse(request.Text).toString() };
//        }
//    }
//}
