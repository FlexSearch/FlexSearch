module Parsing

open System
open System.IO
open System.Runtime.Serialization
open com.calitha.goldparser.lalr
open com.calitha.commons

type SymbolConstants =    
| SYMBOL_EOF              =  0 // (EOF)
| SYMBOL_ERROR            =  1 // (Error)
| SYMBOL_WHITESPACE       =  2 // Whitespace
| SYMBOL_LPAREN           =  3 // '('
| SYMBOL_RPAREN           =  4 // ')'
| SYMBOL_COMMA            =  5 // ','
| SYMBOL_LBRACKET         =  6 // '['
| SYMBOL_RBRACKET         =  7 // ']'
| SYMBOL_AND              =  8 // AND
| SYMBOL_BOOST            =  9 // BOOST
| SYMBOL_IDENTIFIER       = 10 // Identifier
| SYMBOL_INTEGER          = 11 // Integer
| SYMBOL_NOT              = 12 // NOT
| SYMBOL_OR               = 13 // OR
| SYMBOL_VALUE            = 14 // Value
| SYMBOL_BOOSTIDENTIFIER  = 15 // <BoostIdentifier>
| SYMBOL_LIST             = 16 // <List>
| SYMBOL_PREDICATEAND     = 17 // <PredicateAnd>
| SYMBOL_PREDICATECOMPARE = 18 // <PredicateCompare>
| SYMBOL_PREDICATENOT     = 19 // <PredicateNot>
| SYMBOL_PREDICATEOR      = 20 // <PredicateOr>
| SYMBOL_VALUEIDENTIFIER  = 21 // <ValueIdentifier>

type RuleConstants =
| RULE_PREDICATEOR_OR                         =  0 // <PredicateOr> ::= <PredicateAnd> OR <PredicateAnd>
| RULE_PREDICATEOR                            =  1 // <PredicateOr> ::= <PredicateAnd>
| RULE_PREDICATEAND_AND                       =  2 // <PredicateAnd> ::= <PredicateNot> AND <PredicateNot>
| RULE_PREDICATEAND                           =  3 // <PredicateAnd> ::= <PredicateNot>
| RULE_PREDICATENOT_NOT                       =  4 // <PredicateNot> ::= NOT <PredicateCompare>
| RULE_PREDICATENOT                           =  5 // <PredicateNot> ::= <PredicateCompare>
| RULE_PREDICATECOMPARE_IDENTIFIER_IDENTIFIER =  6 // <PredicateCompare> ::= Identifier Identifier <ValueIdentifier> <BoostIdentifier>
| RULE_PREDICATECOMPARE_LPAREN_RPAREN         =  7 // <PredicateCompare> ::= '(' <PredicateOr> ')'
| RULE_VALUEIDENTIFIER_VALUE                  =  8 // <ValueIdentifier> ::= Value
| RULE_VALUEIDENTIFIER_LBRACKET_RBRACKET      =  9 // <ValueIdentifier> ::= '[' <List> ']'
| RULE_LIST_COMMA_VALUE                       = 10 // <List> ::= <List> ',' Value
| RULE_LIST_VALUE                             = 11 // <List> ::= Value
| RULE_BOOSTIDENTIFIER_BOOST_INTEGER          = 12 // <BoostIdentifier> ::= BOOST Integer
| RULE_BOOSTIDENTIFIER                        = 13 // <BoostIdentifier> ::= 

type Token =
| NonterminalToken of RuleConstants * List<Token>
| TerminalToken of SymbolConstants * string 

type MyParser(filename : string) as self =
    [<DefaultValue>]val mutable parser : com.calitha.goldparser.LALRParser

    do
        let stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)
        self.Init(stream)
        stream.Close()

    member public this.Init (stream : Stream) =     
        let reader = new com.calitha.goldparser.CGTReader(stream)
        this.parser <- reader.CreateNewParser()
        this.parser.TrimReductions <- false
        this.parser.StoreTokens <- com.calitha.goldparser.LALRParser.StoreTokensMode.NoUserObject

        //parser.OnTokenError += new LALRParser.TokenErrorHandler(TokenErrorEvent);
        //parser.OnParseError += new LALRParser.ParseErrorHandler(ParseErrorEvent);

    member this.CreateObject(token : Token) : Object =
        match token with
        | TerminalToken(symbol,text) ->
            match symbol with
            //(EOF)
            | SymbolConstants.SYMBOL_EOF              -> failwith("NotImplemented")
            //(Error)
            | SymbolConstants.SYMBOL_ERROR            -> failwith("NotImplemented")
            //Whitespace
            | SymbolConstants.SYMBOL_WHITESPACE       -> failwith("NotImplemented")
            //'('
            | SymbolConstants.SYMBOL_LPAREN           -> failwith("NotImplemented")
            //')'
            | SymbolConstants.SYMBOL_RPAREN           -> failwith("NotImplemented")
            //','
            | SymbolConstants.SYMBOL_COMMA            -> failwith("NotImplemented")
            //'['
            | SymbolConstants.SYMBOL_LBRACKET         -> failwith("NotImplemented")
            //']'
            | SymbolConstants.SYMBOL_RBRACKET         -> failwith("NotImplemented")
            //AND
            | SymbolConstants.SYMBOL_AND              -> failwith("NotImplemented")
            //BOOST
            | SymbolConstants.SYMBOL_BOOST            -> failwith("NotImplemented")
            //Identifier
            | SymbolConstants.SYMBOL_IDENTIFIER       -> failwith("NotImplemented")
            //Integer
            | SymbolConstants.SYMBOL_INTEGER          -> failwith("NotImplemented")
            //NOT
            | SymbolConstants.SYMBOL_NOT              -> failwith("NotImplemented")
            //OR
            | SymbolConstants.SYMBOL_OR               -> failwith("NotImplemented")
            //Value
            | SymbolConstants.SYMBOL_VALUE            -> failwith("NotImplemented")
            //<BoostIdentifier>
            | SymbolConstants.SYMBOL_BOOSTIDENTIFIER  -> failwith("NotImplemented")
            //<List>
            | SymbolConstants.SYMBOL_LIST             -> failwith("NotImplemented")
            //<PredicateAnd>
            | SymbolConstants.SYMBOL_PREDICATEAND     -> failwith("NotImplemented")
            //<PredicateCompare>
            | SymbolConstants.SYMBOL_PREDICATECOMPARE -> failwith("NotImplemented")
            //<PredicateNot>
            | SymbolConstants.SYMBOL_PREDICATENOT     -> failwith("NotImplemented")
            //<PredicateOr>
            | SymbolConstants.SYMBOL_PREDICATEOR      -> failwith("NotImplemented")
            //<ValueIdentifier>
            | SymbolConstants.SYMBOL_VALUEIDENTIFIER  -> failwith("NotImplemented")
            | _ -> failwith("Unknown symbol")
        | NonterminalToken(rule,tokens) ->
            match rule with
            //<PredicateOr> ::= <PredicateAnd> OR <PredicateAnd>
            | RuleConstants.RULE_PREDICATEOR_OR                         -> failwith("NotImplemented")
            //<PredicateOr> ::= <PredicateAnd>
            | RuleConstants.RULE_PREDICATEOR                            -> failwith("NotImplemented")
            //<PredicateAnd> ::= <PredicateNot> AND <PredicateNot>
            | RuleConstants.RULE_PREDICATEAND_AND                       -> failwith("NotImplemented")
            //<PredicateAnd> ::= <PredicateNot>
            | RuleConstants.RULE_PREDICATEAND                           -> failwith("NotImplemented")
            //<PredicateNot> ::= NOT <PredicateCompare>
            | RuleConstants.RULE_PREDICATENOT_NOT                       -> failwith("NotImplemented")
            //<PredicateNot> ::= <PredicateCompare>
            | RuleConstants.RULE_PREDICATENOT                           -> failwith("NotImplemented")
            //<PredicateCompare> ::= Identifier Identifier <ValueIdentifier> <BoostIdentifier>
            | RuleConstants.RULE_PREDICATECOMPARE_IDENTIFIER_IDENTIFIER -> failwith("NotImplemented")
            //<PredicateCompare> ::= '(' <PredicateOr> ')'
            | RuleConstants.RULE_PREDICATECOMPARE_LPAREN_RPAREN         -> failwith("NotImplemented")
            //<ValueIdentifier> ::= Value
            | RuleConstants.RULE_VALUEIDENTIFIER_VALUE                  -> failwith("NotImplemented")
            //<ValueIdentifier> ::= '[' <List> ']'
            | RuleConstants.RULE_VALUEIDENTIFIER_LBRACKET_RBRACKET      -> failwith("NotImplemented")
            //<List> ::= <List> ',' Value
            | RuleConstants.RULE_LIST_COMMA_VALUE                       -> failwith("NotImplemented")
            //<List> ::= Value
            | RuleConstants.RULE_LIST_VALUE                             -> failwith("NotImplemented")
            //<BoostIdentifier> ::= BOOST Integer
            | RuleConstants.RULE_BOOSTIDENTIFIER_BOOST_INTEGER          -> failwith("NotImplemented")
            //<BoostIdentifier> ::= 
            | RuleConstants.RULE_BOOSTIDENTIFIER                        -> failwith("NotImplemented")
            | _ -> failwith("Unknown rule")  

    member this.Parse(source : string) : Object =
        let rec ToX (token : com.calitha.goldparser.Token)  : Token =
            match token with
                | :? com.calitha.goldparser.TerminalToken -> let t = token :?> com.calitha.goldparser.TerminalToken
                                                             TerminalToken(enum t.Symbol.Id,t.Text)

                | :? com.calitha.goldparser.NonterminalToken -> let t = token :?> com.calitha.goldparser.NonterminalToken
                                                                NonterminalToken(enum t.Rule.Id, [for tok in t.Tokens -> ToX(tok)] )

                | _ -> failwith("unknown token type")

        let token = this.parser.Parse(source)
        let xtoken = ToX(token)
        let obj = this.CreateObject(xtoken)
        obj
