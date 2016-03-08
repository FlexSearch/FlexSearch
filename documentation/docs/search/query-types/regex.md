# Regex operator

{% include 'partials/data_notice.md' %}

A fast regular expression query based on the `org.apache.lucene.util.automaton`
package. Comparisons are fast.

The term dictionary is enumerated in an intelligent way, to avoid comparisons.
The supported syntax is documented in the Java RegExp class.

<div class="note">
This query can be slow, as it needs to iterate over many terms. In order to
prevent extremely slow RegexpQueries, a Regexp term should not start with the
expression <code>*</code>.
</div>

<div class="tip">
Regex supports <code>regex</code> operator.
</div>

<div class="note">
Regex query does not go through analysis phase as the analyzer would remove the
special characters. This will convert the input to lowercase before comparison.
</div>

## Query Examples

The following search query matches all the documents containing `silk` and `milk`.

{% include 'data/post-indices-search-regex-1.md'%}
