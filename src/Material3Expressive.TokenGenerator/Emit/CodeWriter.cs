using System;
using System.Text;

namespace Material3Expressive.TokenGenerator.Emit;

/// <summary>An indent-tracking string builder, so the emitters read like the code they produce.</summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;
    private bool _atLineStart = true;

    public CodeWriter Line(string? text = null)
    {
        if (!string.IsNullOrEmpty(text))
            Write(text!);

        _builder.Append('\n');
        _atLineStart = true;
        return this;
    }

    public CodeWriter Write(string text)
    {
        if (_atLineStart && text.Length > 0)
        {
            _builder.Append(' ', _indent * 4);
            _atLineStart = false;
        }

        _builder.Append(text);
        return this;
    }

    /// <summary>Emits an XML doc comment, splitting on newlines so multi-line summaries stay legal.</summary>
    public CodeWriter Doc(string tag, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return this;

        var lines = content!.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 1)
            return Line($"/// <{tag}>{Escape(lines[0].Trim())}</{tag}>");

        Line($"/// <{tag}>");
        foreach (var line in lines)
            Line($"/// {Escape(line.Trim())}");

        return Line($"/// </{tag}>");
    }

    /// <summary>
    /// Opens a braced block. An empty header continues the line already written, and
    /// <paramref name="terminator" /> covers blocks that are expressions rather than statements.
    /// </summary>
    public IDisposable Block(string header, string terminator = "}")
    {
        if (header.Length > 0)
            Line(header);

        Line("{");
        _indent++;
        return new Closer(this, terminator);
    }

    public IDisposable Indent()
    {
        _indent++;
        return new Closer(this, null);
    }

    public override string ToString()
    {
        return _builder.ToString();
    }

    private static string Escape(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private sealed class Closer : IDisposable
    {
        private readonly CodeWriter _writer;
        private readonly string? _terminator;

        public Closer(CodeWriter writer, string? terminator)
        {
            _writer = writer;
            _terminator = terminator;
        }

        public void Dispose()
        {
            _writer._indent--;
            if (_terminator is { })
                _writer.Line(_terminator);
        }
    }
}
