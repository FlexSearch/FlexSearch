---
title: Query Format
layout: docs.html
---

FlexSearch utilizes custom query format inspired by SQL. This is done to reduce 
the learning curve when moving to FlexSearch as most of the modern day 
programmers have written SQL at some point of time in there life.

The below is the query format:

```
<search_condition> ::= 
    { [ NOT ] <predicate> | ( <search_condition> ) } 
    [ { AND | OR } [ NOT ] { <predicate> | ( <search_condition> ) } ] 
[ ,...n ]

<predicate> ::= <field name> <operator> <values>	

<values> ::= <value>
            |   <value> <options>						

<value> ::= <single value>
            | <list values>

<single value> ::= '<any_search_value>'
                | `<any_search_value> <escape> <any_search_value>'

<escape> ::= \\'

<list values> = [ <single value> , <single value> , ..n ]

<options> ::= { <parameter key> : '<parameter value>' , <parameter key> : '<parameter value>', ...n }
```

The parser implements operator precedence as NOT >> AND >> OR.

FlexSearch supports a number of query operators, more explanation about these 
can be accessed from the Query Types section.

