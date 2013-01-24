﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Razorvine.Serpent
{
	public class Ast
	{
		public INode Root;
			
		public interface INode
		{
		}
		
		public struct PrimitiveNode<T> : INode, IComparable<PrimitiveNode<T>> where T: IComparable
		{
			public T Value;
			public PrimitiveNode(T value)
			{
				this.Value=value;
			}
			
			#region Equals and GetHashCode implementation
			public override int GetHashCode()
			{
				return Value!=null? Value.GetHashCode() : 0;
			}
			
			public override bool Equals(object obj)
			{
				return (obj is Ast.PrimitiveNode<T>) &&
					Equals(Value, ((Ast.PrimitiveNode<T>)obj).Value);
			}

			public bool Equals(Ast.PrimitiveNode<T> other)
			{
				return object.Equals(this.Value, other.Value);
			}
			
			public static bool operator ==(Ast.PrimitiveNode<T> lhs, Ast.PrimitiveNode<T> rhs)
			{
				return lhs.Equals(rhs);
			}
			
			public static bool operator !=(Ast.PrimitiveNode<T> lhs, Ast.PrimitiveNode<T> rhs)
			{
				return !(lhs == rhs);
			}
			#endregion

			public int CompareTo(PrimitiveNode<T> other)
			{
				return Value.CompareTo(other.Value);
			}
			
			public override string ToString()
			{
				return string.Format("[PrimitiveNode Type={0} Value={1}]", typeof(T), Value);
			}

		}
		
		public struct ComplexNumberNode: INode
		{
			public double Real;
			public double Imaginary;
		}
		
		public class NoneNode: INode
		{
			public static NoneNode Instance = new NoneNode();
			private NoneNode()
			{
			}
		}
		
		public abstract class SequenceNode: INode
		{
			public List<INode> Elements = new List<INode>();

			public override int GetHashCode()
			{
				int hashCode = Elements.GetHashCode();
				unchecked {
					foreach(var elt in Elements)
						hashCode += 1000000007 * elt.GetHashCode();
				}
				return hashCode;
			}
		}
		
		public class TupleNode : SequenceNode
		{
		}

		public class ListNode : SequenceNode
		{
		}
		
		public class SetNode : SequenceNode
		{
		}
		
		public class DictNode : INode
		{
			public IList<KeyValuePair<INode, INode>> Elements = new List<KeyValuePair<INode, INode>>();
			
			public override bool Equals(object obj)
			{
				Ast.DictNode other = obj as Ast.DictNode;
				if (other == null)
					return false;
				return this.Elements.Equals(other.Elements);
			}
			
			public override int GetHashCode()
			{
				int hashCode = Elements.GetHashCode();
				unchecked {
					foreach(var elt in Elements)
						hashCode += 1000000007 * elt.GetHashCode();
				}
				return hashCode;
			}			
		}
	}

	public class Parser
	{
		public Ast Parse(byte[] serialized)
		{
			return Parse(Encoding.UTF8.GetString(serialized));
		}
		
		public Ast Parse(string expression)
		{
			Ast ast=new Ast();
			if(string.IsNullOrEmpty(expression))
				return ast;
			
			SeekableStringReader sr = new SeekableStringReader(expression);
			try {
				ast.Root = ParseExpr(sr);
				return ast;
			} catch (ParseException x) {
				throw new ParseException(x.Message + " (at position "+sr.Bookmark()+")", x);
			}
		}
		
/*
expr            =  single | compound .
single          =  int | float | complex | string | bool | none .
compound        =  tuple | dict | list | set .
*/

		protected Ast.INode ParseExpr(SeekableStringReader sr)
		{
			// expr = single | compound
			sr.SkipWhitespace();
			char c = sr.Peek();
			if(c=='{' || c=='[' || c=='(')
				return ParseCompound(sr);
			return ParseSingle(sr);
		}
		
		Ast.SequenceNode ParseCompound(SeekableStringReader sr)
		{
			throw new NotImplementedException();
		}
		
		protected Ast.INode ParseSingle(SeekableStringReader sr)
		{
			// single =  int | float | complex | string | bool | none .
			switch(sr.Peek())
			{
				case 'N':
					return ParseNone(sr);
				case 'T':
				case 'F':
					return ParseBool(sr);
				case '\'':
				case '"':
					return ParseString(sr);
			}
			// @todo int or float or complex.
			int bookmark = sr.Bookmark();
			try {
				return ParseComplex(sr);
			} catch (ParseException) {
				sr.FlipBack(bookmark);
				try {
					return ParseFloat(sr);
				} catch (ParseException) {
					sr.FlipBack(bookmark);
					return ParseInt(sr);
				}
			}
		}
		
		Ast.PrimitiveNode<int> ParseInt(SeekableStringReader sr)
		{
			// int =  ['-'] digitnonzero {digit} .
			int sign=1;
			if(sr.Peek()=='-')
			{
				sign = -1;
				sr.Read();
			}
			
			// @todo optimize ... sr.ReadUntilNot(.......)
			StringBuilder intstring = new StringBuilder();
			char nonzerodigit = sr.Read();
			if(nonzerodigit<='1' || nonzerodigit>='9')
				throw new ParseException("expected digit 1..9");
			intstring.Append(nonzerodigit);
			while(sr.HasMore())
			{
				char digit = sr.Read();
				if(digit>='0' && digit<='9')
					intstring.Append(digit);
				else
					break;
			}
			int value = int.Parse(intstring.ToString());
			return new Ast.PrimitiveNode<int>(sign*value);
		}

		Ast.PrimitiveNode<double> ParseFloat(SeekableStringReader sr)
		{
			sr.Read(2);
			throw new ParseException("float not implemented");
		}

		Ast.ComplexNumberNode ParseComplex(SeekableStringReader sr)
		{
			//complex         = complextuple | imaginary .
			//imaginary       = ['+' | '-' ] ( float | int ) 'j' .
			//complextuple    = '(' ( float | int ) imaginary ')' .
			if(sr.Peek()=='(')
			{
				// complextuple
				sr.Read();
				string numberstr = sr.ReadUntil(new char[] {'+', '-'});
				double realpart = double.Parse(numberstr, CultureInfo.InvariantCulture);
				double imaginarypart = ParseImaginaryPart(sr);
				if(sr.Peek()!=')')
					throw new ParseException("expected ) to close a complex number");
				return new Ast.ComplexNumberNode()
					{
						Real = realpart,
						Imaginary = imaginarypart
					};
			}
			else
			{
				// imaginary
				double imag = ParseImaginaryPart(sr);
				return new Ast.ComplexNumberNode()
					{
						Real=0,
						Imaginary=imag
					};
			}
		}
		
		double ParseImaginaryPart(SeekableStringReader sr)
		{
			//imaginary       = ['+' | '-' ] ( float | int ) 'j' .
			char signchr = sr.Read();
			double sign;
			if(signchr=='+')
				sign=1.0;
			else if(signchr=='-')
				sign=-1.0;
			else
				throw new ParseException("expected +/- at start of imaginary part");
			
			string numberstr = sr.ReadUntil('j');
			double value = double.Parse(numberstr, CultureInfo.CurrentCulture);
			return sign*value;
		}
		
		Ast.PrimitiveNode<string> ParseString(SeekableStringReader sr)
		{
			char quotechar = sr.Read();   // ' or "
			StringBuilder sb = new StringBuilder(10);
			while(sr.HasMore())
			{
				char c = sr.Read();
				if(c=='\\')
				{
					// backslash unescape
					c = sr.Read();
					switch(c)
					{
						case '\\':
							sb.Append('\\');
							break;
						case '\'':
							sb.Append('\'');
							break;
						case '"':
							sb.Append('"');
							break;
						case 'a':
							sb.Append('\a');
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'v':
							sb.Append('\v');
							break;
						default:
							sb.Append(c);
							break;
					}
				}
				else if(c==quotechar)
				{
					// end of string
					return new Ast.PrimitiveNode<string>(sb.ToString());
				}
				else
				{
					sb.Append(c);
				}
			}
			throw new ParseException("unclosed string");
		}
		
		Ast.PrimitiveNode<bool> ParseBool(SeekableStringReader sr)
		{
			// True,False
			string b = sr.ReadUntil('e');
			if(b=="Tru")
				return new Ast.PrimitiveNode<bool>(true);
			if(b=="Fals")
				return new Ast.PrimitiveNode<bool>(false);
			throw new ParseException("expected bool, True or False");
		}
		
		Ast.NoneNode ParseNone(SeekableStringReader sr)
		{
			// None
			string n = sr.ReadUntil('e');
			if(n=="Non")
				return Ast.NoneNode.Instance;
			throw new ParseException("expected None");
		}
	}
}

