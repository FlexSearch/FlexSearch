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
        | SYMBOL_EOF              =  0 // (EOF)
        | SYMBOL_ERROR            =  1 // (Error)
        | SYMBOL_WHITESPACE       =  2 // Whitespace
        | SYMBOL_LPARAN           =  3 // '('
        | SYMBOL_RPARAN           =  4 // ')'
        | SYMBOL_COMMA            =  5 // ','
        | SYMBOL_LBRACKET         =  6 // '['
        | SYMBOL_RBRACKET         =  7 // ']'
        | SYMBOL_AND              =  8 // AND
        | SYMBOL_BOOST            =  9 // Boost
        | SYMBOL_IDENTIFIER       = 10 // Identifier
        | SYMBOL_INTEGER          = 11 // Integer
        | SYMBOL_NOT              = 12 // Not
        | SYMBOL_OR               = 13 // OR
        | SYMBOL_VALUE            = 14 // Value
        | SYMBOL_AND_EXPRESSION   = 15 // <And_Expression>
        | SYMBOL_BOOST_IDENTIFIER = 16 // <Boost_Identifier>
        | SYMBOL_CONDITION        = 17 // <Condition>
        | SYMBOL_EXPRESSION       = 18 // <Expression>
        | SYMBOL_LIST             = 19 // <List>
        | SYMBOL_NOT_IDENTIFIER   = 20 // <Not_Identifier>
        | SYMBOL_VALUE_IDENTIFIER = 21 // <Value_Identifier>
    
    type RuleConstants = 
        | RULE_EXPRESSION_OR                      =  0 // <Expression> ::= <And_Expression> OR <Expression>
        | RULE_EXPRESSION                         =  1 // <Expression> ::= <And_Expression>
        | RULE_AND_EXPRESSION_AND                 =  2 // <And_Expression> ::= <Condition> AND <And_Expression>
        | RULE_AND_EXPRESSION                     =  3 // <And_Expression> ::= <Condition>
        | RULE_CONDITION_IDENTIFIER_IDENTIFIER    =  4 // <Condition> ::= Identifier <Not_Identifier> Identifier <Value_Identifier> <Boost_Identifier>
        | RULE_CONDITION_LPARAN_RPARAN            =  5 // <Condition> ::= '(' <Expression> ')'
        | RULE_VALUE_IDENTIFIER_VALUE             =  6 // <Value_Identifier> ::= Value
        | RULE_VALUE_IDENTIFIER_LBRACKET_RBRACKET =  7 // <Value_Identifier> ::= '[' <List> ']'
        | RULE_LIST_COMMA_VALUE                   =  8 // <List> ::= <List> ',' Value
        | RULE_LIST_VALUE                         =  9 // <List> ::= Value
        | RULE_NOT_IDENTIFIER_NOT                 = 10 // <Not_Identifier> ::= Not
        | RULE_NOT_IDENTIFIER                     = 11 // <Not_Identifier> ::= 
        | RULE_BOOST_IDENTIFIER_BOOST_INTEGER     = 12 // <Boost_Identifier> ::= Boost Integer
        | RULE_BOOST_IDENTIFIER                   = 13 // <Boost_Identifier> ::= 
    
    type Token = 
        | NonterminalToken of RuleConstants * List<Token>
        | TerminalToken of SymbolConstants * string
    
    let CreateObject(token : Token) : Object = 
        match token with
        | TerminalToken(symbol, text) -> 
            match symbol with
            //(EOF)
            | SymbolConstants.SYMBOL_EOF -> failwith ("NotImplemented")
            //(Error)
            | SymbolConstants.SYMBOL_ERROR -> failwith ("NotImplemented")
            //Whitespace
            | SymbolConstants.SYMBOL_WHITESPACE -> failwith ("NotImplemented")
            //'('
            | SymbolConstants.SYMBOL_LPARAN -> failwith ("NotImplemented")
            //')'
            | SymbolConstants.SYMBOL_RPARAN -> failwith ("NotImplemented")
            //','
            | SymbolConstants.SYMBOL_COMMA -> failwith ("NotImplemented")
            //'['
            | SymbolConstants.SYMBOL_LBRACKET -> failwith ("NotImplemented")
            //']'
            | SymbolConstants.SYMBOL_RBRACKET -> failwith ("NotImplemented")
            //AND
            | SymbolConstants.SYMBOL_AND -> failwith ("NotImplemented")
            //Boost
            | SymbolConstants.SYMBOL_BOOST -> failwith ("NotImplemented")
            //Identifier
            | SymbolConstants.SYMBOL_IDENTIFIER -> failwith ("NotImplemented")
            //Integer
            | SymbolConstants.SYMBOL_INTEGER -> failwith ("NotImplemented")
            //Not
            | SymbolConstants.SYMBOL_NOT -> failwith ("NotImplemented")
            //OR
            | SymbolConstants.SYMBOL_OR -> failwith ("NotImplemented")
            //Value
            | SymbolConstants.SYMBOL_VALUE -> failwith ("NotImplemented")
            //<And_Expression>
            | SymbolConstants.SYMBOL_AND_EXPRESSION -> failwith ("NotImplemented")
            //<Boost_Identifier>
            | SymbolConstants.SYMBOL_BOOST_IDENTIFIER -> failwith ("NotImplemented")
            //<Condition>
            | SymbolConstants.SYMBOL_CONDITION -> failwith ("NotImplemented")
            //<Expression>
            | SymbolConstants.SYMBOL_EXPRESSION -> failwith ("NotImplemented")
            //<List>
            | SymbolConstants.SYMBOL_LIST -> failwith ("NotImplemented")
            //<Not_Identifier>
            | SymbolConstants.SYMBOL_NOT_IDENTIFIER -> failwith ("NotImplemented")
            //<Value_Identifier>
            | SymbolConstants.SYMBOL_VALUE_IDENTIFIER -> failwith ("NotImplemented")
            | _ -> failwith ("Unknown symbol")
        | NonterminalToken(rule, tokens) -> 
            match rule with
            //<Expression> ::= <And_Expression> OR <Expression>
            | RuleConstants.RULE_EXPRESSION_OR -> failwith ("NotImplemented")
            //<Expression> ::= <And_Expression>
            | RuleConstants.RULE_EXPRESSION -> failwith ("NotImplemented")
            //<And_Expression> ::= <Condition> AND <And_Expression>
            | RuleConstants.RULE_AND_EXPRESSION_AND -> failwith ("NotImplemented")
            //<And_Expression> ::= <Condition>
            | RuleConstants.RULE_AND_EXPRESSION -> failwith ("NotImplemented")
            //<Condition> ::= Identifier <Not_Identifier> Identifier <Value_Identifier> <Boost_Identifier>
            | RuleConstants.RULE_CONDITION_IDENTIFIER_IDENTIFIER -> failwith ("NotImplemented")
            //<Condition> ::= '(' <Expression> ')'
            | RuleConstants.RULE_CONDITION_LPARAN_RPARAN -> failwith ("NotImplemented")
            //<Value_Identifier> ::= Value
            | RuleConstants.RULE_VALUE_IDENTIFIER_VALUE -> failwith ("NotImplemented")
            //<Value_Identifier> ::= '[' <List> ']'
            | RuleConstants.RULE_VALUE_IDENTIFIER_LBRACKET_RBRACKET -> failwith ("NotImplemented")
            //<List> ::= <List> ',' Value
            | RuleConstants.RULE_LIST_COMMA_VALUE -> failwith ("NotImplemented")
            //<List> ::= Value
            | RuleConstants.RULE_LIST_VALUE -> failwith ("NotImplemented")
            //<Not_Identifier> ::= Not
            | RuleConstants.RULE_NOT_IDENTIFIER_NOT -> failwith ("NotImplemented")
            //<Not_Identifier> ::= 
            | RuleConstants.RULE_NOT_IDENTIFIER -> failwith ("NotImplemented")
            //<Boost_Identifier> ::= Boost Integer
            | RuleConstants.RULE_BOOST_IDENTIFIER_BOOST_INTEGER -> failwith ("NotImplemented")
            //<Boost_Identifier> ::= 
            | RuleConstants.RULE_BOOST_IDENTIFIER -> failwith ("NotImplemented")
            | _ -> failwith ("Unknown rule")
    
    type Parser() as self = 
        [<DefaultValue>] val mutable parser : com.calitha.goldparser.LALRParser
        do self.Init()
        
        member public this.Init() = 
            let assem = Assembly.GetExecutingAssembly()
            use stream = assem.GetManifestResourceStream("FlexSearchGrammar.cgt")
            //let filePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "FlexSearchGrammar.cgt")
            //if File.Exists(filePath) <> true then failwithf "Unable to load the grammar."
            try 
                let reader = new com.calitha.goldparser.CGTReader(stream)
                this.parser <- reader.CreateNewParser()
                this.parser.TrimReductions <- false
                this.parser.StoreTokens <- com.calitha.goldparser.LALRParser.StoreTokensMode.NoUserObject
            with ex -> raise ex
        
        //parser.OnTokenError += new LALRParser.TokenErrorHandler(TokenErrorEvent);
        //parser.OnParseError += new LALRParser.ParseErrorHandler(ParseErrorEvent);
        member this.Parse(source : string) = 
            let rec ToX(token : com.calitha.goldparser.Token) : Token = 
                match token with
                | :? com.calitha.goldparser.TerminalToken -> 
                    let t = token :?> com.calitha.goldparser.TerminalToken
                    TerminalToken(enum t.Symbol.Id, t.Text)
                | :? com.calitha.goldparser.NonterminalToken -> 
                    let t = token :?> com.calitha.goldparser.NonterminalToken
                    NonterminalToken(enum t.Rule.Id, 
                                     [ for tok in t.Tokens -> ToX(tok) ])
                | _ -> failwith ("unknown token type")
            
            let token = this.parser.Parse(source)
            ToX(token)
// let obj = this.CreateObject(xtoken)
//obj
