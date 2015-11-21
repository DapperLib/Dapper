<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html"/>
  <xsl:key name="tests-by-class" match="collection/test" use="@type" />
  <xsl:template match="/">
    <xsl:text disable-output-escaping="yes"><![CDATA[<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">]]></xsl:text>
    <html>
      <head>
        <title>xUnit.net Test Results</title>
        <style type="text/css">
          body { font-family: Calibri, Verdana, Arial, sans-serif; background-color: White; color: Black; }
          h2,h3,h4,h5 { margin: 0; padding: 0; }
          h3 { font-weight: normal; }
          h4 { margin: 0.5em 0; }
          h5 { font-weight: normal; font-style: italic; margin-bottom: 0.75em; }
          h6 { font-size: 0.9em; font-weight: bold; margin: 0.5em 0 0 0.75em; padding: 0; }
          pre,table { font-family: Consolas; font-size: 0.8em; margin: 0 0 0 1em; padding: 0; }
          table { padding-bottom: 0.25em; }
          th { padding: 0 0.5em; border-right: 1px solid #bbb; text-align: left; }
          td { padding-left: 0.5em; }
          .divided { border-top: solid 1px #f0f5fa; padding-top: 0.5em; }
          .row, .altrow { padding: 0.1em 0.3em; }
          .row { background-color: #f0f5fa; }
          .altrow { background-color: #e1ebf4; }
          .success, .failure, .skipped { font-family: Arial Unicode MS; font-weight: normal; float: left; width: 1em; display: block; }
          .success { color: #0c0; }
          .failure { color: #c00; }
          .skipped { color: #cc0; }
          .timing { float: right; }
          .indent { margin: 0.25em 0 0.5em 2em; }
          .clickable { cursor: pointer; }
          .testcount { font-size: 85%; }
        </style>
        <script language="javascript">
          function ToggleClass(id) {
            var elem = document.getElementById(id);
            if (elem.style.display == "none") {
              elem.style.display = "block";
            }
            else {
              elem.style.display = "none";
            }
          }
        </script>
      </head>
      <body>
        <h3 class="divided">
          <b>Assemblies Run</b>
        </h3>
        <xsl:apply-templates select="//assembly"/>
        <h3 class="divided">
          <b>Summary</b>
        </h3>
        <div>
          Tests run: <a href="#all"><b><xsl:value-of select="sum(//assembly/@total)"/></b></a> &#160;
          <xsl:if test="sum(//assembly/@errors) > 0">
            Errors: <a href="#errors"><b><xsl:value-of select="sum(//assembly/@errors)"/></b></a>,
          </xsl:if>
          <xsl:if test="sum(//assembly/@failed) > 0">
            Failures: <a href="#failures"><b><xsl:value-of select="sum(//assembly/@failed)"/></b></a>,
          </xsl:if>
          <xsl:if test="sum(//assembly/@skipped) > 0">
            Skipped: <a href="#skipped"><b><xsl:value-of select="sum(//assembly/@skipped)"/></b></a>,
          </xsl:if>
          Run time: <b><xsl:value-of select="format-number(sum(//assembly/@time), '0.000')"/>s</b>
        </div>
        <xsl:if test="//assembly/errors/error">
          <br />
          <h2>
            <a id="errors"></a>Errors
          </h2>
          <xsl:apply-templates select="//assembly/errors"/>
        </xsl:if>
        <xsl:if test="//assembly/collection/test[@result='Fail']">
          <br />
          <h2>
            <a id="failures"></a>Failed tests
          </h2>
          <xsl:apply-templates select="//assembly/collection/test[@result='Fail']">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </xsl:if>
        <xsl:if test="//assembly/collection/failures/failure">
          <br />
          <h2>
            <a id="failures"></a>Collection failures
          </h2>
          <xsl:apply-templates select="//assembly/collection/failures">
            <xsl:sort select="../@name"/>
          </xsl:apply-templates>
        </xsl:if>
        <xsl:if test="//assembly/@skipped > 0">
          <br />
          <h2>
            <a id="skipped"></a>Skipped tests
          </h2>
          <xsl:apply-templates select="//assembly/collection/test[@result='Skip']">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </xsl:if>
        <br />
        <h2>
          <a id="all"></a>All tests
        </h2>
        <h5>Click test class name to expand/collapse test details</h5>

        <xsl:for-each select="//assembly/collection/test[count(. | key('tests-by-class', @type)[1]) = 1]">
          <xsl:sort select="@type" />
          <h3>
            <span class="timing">
              <xsl:value-of select="format-number(sum(key('tests-by-class', @type)/@time), '0.000')"/>s
            </span>
            <span class="clickable">
              <xsl:attribute name="onclick">ToggleClass('class<xsl:value-of select="generate-id()"/>')</xsl:attribute>
              <xsl:attribute name="ondblclick">ToggleClass('class<xsl:value-of select="generate-id()"/>')</xsl:attribute>
              <xsl:if test="count(key('tests-by-class', @type)[@result='Fail']) > 0">
                <span class="failure">&#x2718;</span>
              </xsl:if>
              <xsl:if test="count(key('tests-by-class', @type)[@result='Fail']) = 0">
                <span class="success">&#x2714;</span>
              </xsl:if>
              &#160;<xsl:value-of select="@type"/>
              &#160;<span class="testcount">
                (<xsl:value-of select="count(key('tests-by-class', @type))"/>&#160;test<xsl:if test="count(key('tests-by-class', @type)) > 1">s</xsl:if>)
              </span>
            </span>
            <br clear="all" />
          </h3>
          <div class="indent">
            <xsl:if test="count(key('tests-by-class', @type)[@result='Fail']) = 0">
              <xsl:attribute name="style">display: none;</xsl:attribute>
            </xsl:if>
            <xsl:attribute name="id">class<xsl:value-of select="generate-id()"/></xsl:attribute>
            <xsl:apply-templates select="key('tests-by-class', @type)">
              <xsl:sort select="@name"/>
            </xsl:apply-templates>
          </div>
        </xsl:for-each>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="assembly">
    <div>
      <xsl:value-of select="@name"/>
    </div>
  </xsl:template>

  <xsl:template match="test">
    <div>
      <xsl:attribute name="class"><xsl:if test="(position() mod 2 = 0)">alt</xsl:if>row</xsl:attribute>
      <xsl:if test="@result!='Skip'">
        <span class="timing">
          <xsl:value-of select="@time"/>s
        </span>
      </xsl:if>
      <xsl:if test="@result='Skip'">
        <span class="timing">Skipped</span>
        <span class="skipped">&#x2762;</span>
      </xsl:if>
      <xsl:if test="@result='Fail'">
        <span class="failure">&#x2718;</span>
      </xsl:if>
      <xsl:if test="@result='Pass'">
        <span class="success">&#x2714;</span>
      </xsl:if>
      &#160;<xsl:value-of select="@name"/>
      <br clear="all" />
      <xsl:if test="child::node()/message">
        <pre><xsl:value-of select="child::node()/message"/></pre>
      </xsl:if>
      <xsl:if test="failure/stack-trace">
        <pre><xsl:value-of select="failure/stack-trace"/></pre>
      </xsl:if>
      <xsl:if test="output">
        <h6>Output:</h6>
        <pre><xsl:value-of select="output"/></pre>
      </xsl:if>
      <xsl:if test="traits">
        <h6>Traits:</h6>
        <table cellspacing="0" cellpadding="0">
          <xsl:apply-templates select="traits/trait"/>
        </table>
      </xsl:if>
    </div>
  </xsl:template>

  <xsl:template match="trait">
    <tr>
      <th><xsl:value-of select="@name"/></th>
      <td><xsl:value-of select="@value"/></td>
    </tr>
  </xsl:template>

  <xsl:template match="failures">
    <h4><xsl:value-of select="../@name"/></h4>
    <xsl:for-each select="failure">
      <div>
        <xsl:attribute name="class"><xsl:if test="(position() mod 2 = 0)">alt</xsl:if>row</xsl:attribute>
        <span class="failure">&#x2718;</span><br clear="all"/>
        <xsl:if test="child::node()/message">
          <pre><xsl:value-of select="child::node()/message"/></pre>
        </xsl:if>
        <xsl:if test="stack-trace">
          <pre><xsl:value-of select="stack-trace"/></pre>
        </xsl:if>
      </div>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="errors">
    <xsl:for-each select="error">
      <div>
        <xsl:attribute name="class">
          <xsl:if test="(position() mod 2 = 0)">alt</xsl:if>row
        </xsl:attribute>
        <span class="failure">&#x2718;</span>
        <xsl:if test="@type='assembly-cleanup'">Test Assembly Cleanup</xsl:if>
        <xsl:if test="@type='test-collection-cleanup'">Test Collection Cleanup</xsl:if>
        <xsl:if test="@type='test-class-cleanup'">Test Class Cleanup</xsl:if>
        <xsl:if test="@type='test-method-cleanup'">Test Method Cleanup</xsl:if>
        <xsl:if test="@type='test-case-cleanup'">Test Case Cleanup</xsl:if>
        <xsl:if test="@type='test-cleanup'">Test Cleanup</xsl:if>
        <xsl:if test="@type='fatal'">Fatal Error</xsl:if>
        <xsl:if test="@name">
          (<xsl:value-of select="@name"/>)
        </xsl:if>
        <br clear="all"/>
        <xsl:if test="child::node()/message">
          <pre><xsl:value-of select="child::node()/message"/></pre>
        </xsl:if>
        <xsl:if test="stack-trace">
          <pre><xsl:value-of select="stack-trace"/></pre>
        </xsl:if>
      </div>
    </xsl:for-each>
  </xsl:template>

</xsl:stylesheet>