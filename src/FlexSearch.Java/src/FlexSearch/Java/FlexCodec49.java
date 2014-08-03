package FlexSearch.Java;
import org.apache.lucene.codecs.*;
import org.apache.lucene.codecs.lucene49.Lucene49Codec;

public final class FlexCodec49 extends FilterCodec {

	private PostingsFormat postingsFormat;

	public FlexCodec49() {
		super("FlexCodec49", new Lucene49Codec());
	}

	public FlexCodec49(PostingsFormat postingsFormat, Codec delegate) {
		super("FlexCodec49", delegate);
		this.postingsFormat = postingsFormat;
	}

	@Override
	public final PostingsFormat postingsFormat() {
		return postingsFormat;
	}

}
