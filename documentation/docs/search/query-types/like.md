# Wildcard operator

{% include 'partials/data_notice.md' %}

Implements the wildcard search query. Supported wildcards are `*`, which matches
any character sequence (including the empty one), and `?`, which matches any
single character. Note this query can be slow, as it needs to iterate over many
terms.

<div class="tip">
In order to prevent extremely slow WildcardQueries, a Wildcard term should not
start with the wildcard *.
</div>

<div class="tip">
Phrase match supports <code>like</code> and <code>%=</code> operators.
</div>

<div class="important">
Like query does not go through analysis phase as the analyzer would remove the
special characters. This will convert the input to lowercase before comparison.
</div>

## Query Examples

The following search query returns all documents with `uni` coming anywhere in
the word.

{% include 'data/post-indices-search-wildcard-1.md'%}

The following query will match any word where it starts with `Unit` followed by
any single character and ends with `d`.

{% include 'data/post-indices-search-wildcard-3.md'%}
