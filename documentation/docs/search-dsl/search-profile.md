# Search Profile

Search profile is an extension of normal searching capability of FlexSearch which
allows central management of search queries. It is also used by background duplicate
matching. Think of it as a way to define a search criteria which is managed at
the server and can be called from various systems without the need to specify the
criteria as the as a part of the query. This allows easy management of queries
across many systems. For example, you can define a query which can detect duplicates
in your customer data, you can call this query from your various systems like
data entry, point of sale etc. if you ever decide to update the criteria you
don't have to redefine the criteria in all the systems.

This is an extremely powerful and useful feature present in the engine. It also
allows you to define various kinds of scripts which can be executed before or
after the main query is processed, this gives you an easy way to extend the
search pipeline.
