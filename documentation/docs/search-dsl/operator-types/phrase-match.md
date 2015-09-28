# Phrase match operator

{% include 'partials/data_notice.md' %}

A Query that matches documents containing a particular sequence of terms.

Parameter |Default |Type |Description
`slop` |1 |int |The number of allowed edits

<div class="tip">
Phrase match supports <code>match</code>.
</div>

## Query Examples

The following search query returns all documents containing the 3 words
`federal parliamentary democracy` is exactly the same order.
{% include 'data/post-indices-search-phrase-1.md'%}

Phrase query also supports `slop` parameter. It behaves similar to the the `slop`
parameter in term query. By default the slop is set to 0 which means match in
exact order. A minimum slop of 2 is required to change the order of the terms.

<div class="important">
Specifying <code>slop</code> in phrase query does not maintain the order of the
terms. The query is reduced to a term query with the terms being in the
specified range of each other.
</div>

{% include 'data/post-indices-search-phrase-2.md'%}

Below query demonstrated the behaviour when slop is used to match the same words
from query 2 but in reverse order.

{% include 'data/post-indices-search-phrase-3.md'%}
