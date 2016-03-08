---
title: Text highlighting
layout: docs.html
---

FlexSearch supports text highlighting across all query types provided correct highlighting options are set in the request query. Text highlighting is supported only for `Highlight` and `Custom` field types.

PreTag and PostTag can be specified and the returned result will contain the matched text between pre and post tags. This is helpful in case the results are to be expressed in a web page.

{{tip '`returnFlatResults` should be set to false in order to get highlight results.'}}

{{accordionStart 'Example'}}
	{{> 'example' post-indices-search-highlighting-1}}
{{accordionEnd}}