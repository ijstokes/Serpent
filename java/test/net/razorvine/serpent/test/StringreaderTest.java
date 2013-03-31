/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent.test;

import static org.junit.Assert.*;
import net.razorvine.serpent.SeekableStringReader;

import org.junit.Test;

public class StringreaderTest
{
	@Test
	public void TestStuff()
	{
		SeekableStringReader s=new SeekableStringReader("hello");
		assertEquals('h', s.Peek());
		assertEquals("hel", s.Peek(3));
		assertEquals('h', s.Read());
		assertEquals("ell", s.Read(3));
		assertEquals("o", s.Peek(999));
		assertEquals("o", s.Read(999));
		s.close();
		
		SeekableStringReader s2 = new SeekableStringReader("    skip.\t\n\rwhitespace.  ");
		s2.SkipWhitespace();
		assertEquals("skip", s2.ReadUntil('.'));
		s2.SkipWhitespace();
		assertEquals("whitespace", s2.ReadUntil('.'));
		s2.SkipWhitespace();
		assertFalse(s2.HasMore());
		s2.Peek();
		// TODO Assert.Throws<IndexOutOfRangeException>(()=>s2.Peek());
		s.close();
	}
	
	@Test
	public void TestRanges()
	{
		SeekableStringReader s = new SeekableStringReader("hello");
		s.Read(-1);
		// TODO Assert.Throws<ParseException>(()=>s.Read(-1));
		assertEquals("hello", s.Read(999));
		s.Read(1);
		// TODO Assert.Throws<ParseException>(()=>s.Read(1));
		s.Rewind(Integer.MAX_VALUE);
		assertTrue(s.HasMore());
		assertEquals("hello", s.Peek(999));
		assertTrue(s.HasMore());
	}
	
	@Test
	public void TestReadUntil()
	{
		SeekableStringReader s = new SeekableStringReader("hello there");
		s.Read();
		assertEquals("ello", s.ReadUntil(' '));
		assertEquals('t', s.Peek());
		
		s.ReadUntil('x');
		// TODO Assert.Throws<ParseException>(()=>s.ReadUntil('x'));
		
		assertEquals("there", s.Rest());
		
		s.Rest();
		// TODO  Assert.Throws<ParseException>(()=>s.Rest());
		
		s.Rewind(Integer.MAX_VALUE);
		assertEquals("hell", s.ReadUntil('x', 'y', 'z', ' ', 'o'));
		
		s.ReadUntil('x', 'y', '@');
		// TODO Assert.Throws<ParseException>(()=>s.ReadUntil('x', 'y', '@'));
	}

	@Test
	public void TestReadWhile()
	{
		SeekableStringReader s = new SeekableStringReader("123.456 foo");
		assertEquals("123.456", s.ReadWhile('0','1','2','3','4','5','6','7','8','9','.'));
		assertEquals("", s.ReadWhile('@'));
		assertEquals(" ", s.ReadWhile(' '));
		assertEquals("foo", s.Rest());
	}
	
	@Test
	public void TestBookmark()
	{
		SeekableStringReader s = new SeekableStringReader("hello");
		s.Read(2);
		int bookmark = s.Bookmark();
		assertEquals("ll", s.Read(2));
		s.FlipBack(bookmark);
		assertEquals("ll", s.Read(2));
		assertEquals("o", s.Read(999));
	}
	
	@Test
	public void TestNesting()
	{
		SeekableStringReader outer = new SeekableStringReader("hello!");
		outer.Read(1);
		SeekableStringReader inner1 = new SeekableStringReader(outer);
		SeekableStringReader inner2 = new SeekableStringReader(outer);
		
		assertEquals("ell", inner1.Read(3));
		assertEquals("el", inner2.Read(2));
		assertEquals("o", inner1.Read(1));
		assertEquals("l", inner2.Read(1));
		assertEquals("e", outer.Read(1));
		assertEquals("o", inner2.Read(1));
		assertEquals("l", outer.Read(1));
		outer.Sync(inner2);
		assertEquals("!", outer.Read(1));
	}

	@Test
	public void TestContext()
	{
		SeekableStringReader s = new SeekableStringReader("abcdefghijklmnopqrstuvwxyz");
		s.Read(10);
		SeekableStringReader.StringContext ctx = s.Context(-1, 5);
		assertEquals("fghij", ctx.left);
		assertEquals("klmno", ctx.right);
		ctx = s.Context(-1, 12);
		assertEquals("abcdefghij", ctx.left);
		assertEquals("klmnopqrstuv", ctx.right);
		s.Read(13);
		ctx = s.Context(-1, 6);
		assertEquals("rstuvw", ctx.left);
		assertEquals("xyz", ctx.right);
		
		ctx=s.Context(5,4);
		assertEquals("bcde", ctx.left);
		assertEquals("fghi", ctx.right);
	}
}
