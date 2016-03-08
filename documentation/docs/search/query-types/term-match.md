# Term match operator

{% include 'partials/data_notice.md' %}

A Query that matches documents containing a term.

Parameter |Default |Type |Description
--- | --- | --- | ---
`clausetype` | and |string (`and`, `or`) |In case more than one term is searched then the query is converted into a number of sub-queries and the clausetype operator is used to determine the matching logic. For example an and clause will match all the terms passed to the query.

Do not use term query for phrase matches as you might get unexpected results.

<div class="tip">
Term match supports both <code>=</code> and <code>eq</code> operator.
</div>

## Query Examples
The following search query returns all documents containing `Wheat` and `Rice`
both, in the `agriproducts` field.

{% include 'data/post-indices-search-term-1.md' %}

Term query also supports matching over multiple terms in the same clause. This
is helpful when you want to check the presence of multiple terms in the same
field. By default FlexSearch will look for the presence of all the passed terms
in the field. So, the behaviour is same as using `and` operator. Below is the
earlier query written without the `and` clause.

{% include 'data/post-indices-search-term-2.md' %}

In order to change the default matching behaviour from `and` to `or`, use the
`clausetype` operator.

{% include 'data/post-indices-search-term-4.md' %}

The above query is same as the following query:

{% include 'data/post-indices-search-term-3.md' %}
