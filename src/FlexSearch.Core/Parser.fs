// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

open System
open System.Resources
open System.Reflection
open System.IO
open System.Runtime.Serialization
open com.calitha.goldparser.lalr
open com.calitha.commons

// ----------------------------------------------------------------------------
/// Default query parser used by Flex
// ----------------------------------------------------------------------------
module FlexParser = 
    
    type SymbolConstants =    
        | SYMBOL_EOF           =  0 // (EOF)
        | SYMBOL_ERROR         =  1 // (Error)
        | SYMBOL_WHITESPACE    =  2 // Whitespace
        | SYMBOL_LPAREN        =  3 // '('
        | SYMBOL_RPAREN        =  4 // ')'
        | SYMBOL_AND           =  5 // AND
        | SYMBOL_BOOST         =  6 // Boost
        | SYMBOL_IDENTIFIER    =  7 // Identifier
        | SYMBOL_INTEGER       =  8 // Integer
        | SYMBOL_NOT           =  9 // Not
        | SYMBOL_OR            = 10 // OR
        | SYMBOL_VALUE         = 11 // Value
        | SYMBOL_ANDEXPRESSION = 12 // <And Expression>
        | SYMBOL_BOOST2        = 13 // <Boost>
        | SYMBOL_CONDITION     = 14 // <Condition>
        | SYMBOL_EXPRESSION    = 15 // <Expression>
        | SYMBOL_NOT2          = 16 // <Not>

    type RuleConstants =
        | RULE_EXPRESSION_OR                         =  0 // <Expression> ::= <And Expression> OR <Expression>
        | RULE_EXPRESSION                            =  1 // <Expression> ::= <And Expression>
        | RULE_ANDEXPRESSION_AND                     =  2 // <And Expression> ::= <Condition> AND <And Expression>
        | RULE_ANDEXPRESSION                         =  3 // <And Expression> ::= <Condition>
        | RULE_CONDITION_IDENTIFIER_IDENTIFIER_VALUE =  4 // <Condition> ::= Identifier <Not> Identifier Value <Boost>
        | RULE_CONDITION_LPAREN_RPAREN               =  5 // <Condition> ::= '(' <Expression> ')'
        | RULE_NOT_NOT                               =  6 // <Not> ::= Not
        | RULE_NOT                                   =  7 // <Not> ::= 
        | RULE_BOOST_BOOST_INTEGER                   =  8 // <Boost> ::= Boost Integer
        | RULE_BOOST                                 =  9 // <Boost> ::= 

    type Token =
        | NonterminalToken of RuleConstants * List<Token>
        | TerminalToken of SymbolConstants * string 
    
     let CreateObject(token : Token) : Object =
        match token with
        | TerminalToken(symbol,text) ->
            match symbol with
            //(EOF)
            | SymbolConstants.SYMBOL_EOF           -> failwith("NotImplemented")
            //(Error)
            | SymbolConstants.SYMBOL_ERROR         -> failwith("NotImplemented")
            //Whitespace
            | SymbolConstants.SYMBOL_WHITESPACE    -> failwith("NotImplemented")
            //'('
            | SymbolConstants.SYMBOL_LPAREN        -> failwith("NotImplemented")
            //')'
            | SymbolConstants.SYMBOL_RPAREN        -> failwith("NotImplemented")
            //AND
            | SymbolConstants.SYMBOL_AND           -> failwith("NotImplemented")
            //Boost
            | SymbolConstants.SYMBOL_BOOST         -> failwith("NotImplemented")
            //Identifier
            | SymbolConstants.SYMBOL_IDENTIFIER    -> failwith("NotImplemented")
            //Integer
            | SymbolConstants.SYMBOL_INTEGER       -> failwith("NotImplemented")
            //Not
            | SymbolConstants.SYMBOL_NOT           -> failwith("NotImplemented")
            //OR
            | SymbolConstants.SYMBOL_OR            -> failwith("NotImplemented")
            //Value
            | SymbolConstants.SYMBOL_VALUE         -> failwith("NotImplemented")
            //<And Expression>
            | SymbolConstants.SYMBOL_ANDEXPRESSION -> failwith("NotImplemented")
            //<Boost>
            | SymbolConstants.SYMBOL_BOOST2        -> failwith("NotImplemented")
            //<Condition>
            | SymbolConstants.SYMBOL_CONDITION     -> failwith("NotImplemented")
            //<Expression>
            | SymbolConstants.SYMBOL_EXPRESSION    -> failwith("NotImplemented")
            //<Not>
            | SymbolConstants.SYMBOL_NOT2          -> failwith("NotImplemented")
            | _ -> failwith("Unknown symbol")
        | NonterminalToken(rule,tokens) ->
            match rule with
            //<Expression> ::= <And Expression> OR <Expression>
            | RuleConstants.RULE_EXPRESSION_OR                         -> failwith("NotImplemented")
            //<Expression> ::= <And Expression>
            | RuleConstants.RULE_EXPRESSION                            -> failwith("NotImplemented")
            //<And Expression> ::= <Condition> AND <And Expression>
            | RuleConstants.RULE_ANDEXPRESSION_AND                     -> failwith("NotImplemented")
            //<And Expression> ::= <Condition>
            | RuleConstants.RULE_ANDEXPRESSION                         -> failwith("NotImplemented")
            //<Condition> ::= Identifier <Not> Identifier Value <Boost>
            | RuleConstants.RULE_CONDITION_IDENTIFIER_IDENTIFIER_VALUE -> failwith("NotImplemented")
            //<Condition> ::= '(' <Expression> ')'
            | RuleConstants.RULE_CONDITION_LPAREN_RPAREN               -> failwith("NotImplemented")
            //<Not> ::= Not
            | RuleConstants.RULE_NOT_NOT                               -> failwith("NotImplemented")
            //<Not> ::= 
            | RuleConstants.RULE_NOT                                   -> failwith("NotImplemented")
            //<Boost> ::= Boost Integer
            | RuleConstants.RULE_BOOST_BOOST_INTEGER                   -> failwith("NotImplemented")
            //<Boost> ::= 
            | RuleConstants.RULE_BOOST                                 -> failwith("NotImplemented")
            | _ -> failwith("Unknown rule")  

        type Parser() as self =
            [<DefaultValue>]val mutable parser : com.calitha.goldparser.LALRParser

            do
                self.Init()

            member public this.Init () = 
                let assem = Assembly.GetExecutingAssembly()
                use stream = assem.GetManifestResourceStream("FlexSearchGrammar.cgt")
                //let filePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "FlexSearchGrammar.cgt")
                //if File.Exists(filePath) <> true then failwithf "Unable to load the grammar."
                
                try
                    let reader = new com.calitha.goldparser.CGTReader(stream)
                    this.parser <- reader.CreateNewParser()
                    this.parser.TrimReductions <- false
                    this.parser.StoreTokens <- com.calitha.goldparser.LALRParser.StoreTokensMode.NoUserObject
                with
                    | ex -> raise ex

                //parser.OnTokenError += new LALRParser.TokenErrorHandler(TokenErrorEvent);
                //parser.OnParseError += new LALRParser.ParseErrorHandler(ParseErrorEvent);

            member this.Parse(source : string) =
                let rec ToX (token : com.calitha.goldparser.Token)  : Token =
                    match token with
                        | :? com.calitha.goldparser.TerminalToken -> let t = token :?> com.calitha.goldparser.TerminalToken
                                                                     TerminalToken(enum t.Symbol.Id,t.Text)

                        | :? com.calitha.goldparser.NonterminalToken -> let t = token :?> com.calitha.goldparser.NonterminalToken
                                                                        NonterminalToken(enum t.Rule.Id, [for tok in t.Tokens -> ToX(tok)] )

                        | _ -> failwith("unknown token type")

                let token = this.parser.Parse(source)
                ToX(token)
            
            // let obj = this.CreateObject(xtoken)
            //obj