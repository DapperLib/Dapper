<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" cdata-section-elements="message stack-trace"/>
  <xsl:key name="tests-by-class" match="collection/test" use="@type" />
  <xsl:template match="/">
    <assemblies>
      <xsl:for-each select="//assembly">
        <assembly>
          <xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
          <xsl:attribute name="configFile"><xsl:value-of select="@config-file"/></xsl:attribute>
          <xsl:attribute name="run-date"><xsl:value-of select="@run-date"/></xsl:attribute>
          <xsl:attribute name="run-time"><xsl:value-of select="@run-time"/></xsl:attribute>
          <xsl:attribute name="time"><xsl:value-of select="@time"/></xsl:attribute>
          <xsl:attribute name="total"><xsl:value-of select="@total"/></xsl:attribute>
          <xsl:attribute name="passed"><xsl:value-of select="@passed"/></xsl:attribute>
          <xsl:attribute name="failed"><xsl:value-of select="@failed"/></xsl:attribute>
          <xsl:attribute name="skipped"><xsl:value-of select="@skipped"/></xsl:attribute>
          <xsl:attribute name="environment"><xsl:value-of select="@environment"/></xsl:attribute>
          <xsl:attribute name="test-framework"><xsl:value-of select="@test-framework"/></xsl:attribute>

          <xsl:for-each select="collection/test[count(. | key('tests-by-class', @type)[1]) = 1]">
            <xsl:sort select="@type" />
            <class>
              <xsl:attribute name="name"><xsl:value-of select="@type"/></xsl:attribute>
              <xsl:attribute name="time"><xsl:value-of select="format-number(sum(key('tests-by-class', @type)/@time), '0.000')"/></xsl:attribute>
              <xsl:attribute name="total"><xsl:value-of select="count(key('tests-by-class', @type))"/></xsl:attribute>
              <xsl:attribute name="passed"><xsl:value-of select="count(key('tests-by-class', @type)[@result='Pass'])"/></xsl:attribute>
              <xsl:attribute name="failed"><xsl:value-of select="count(key('tests-by-class', @type)[@result='Fail'])"/></xsl:attribute>
              <xsl:attribute name="skipped"><xsl:value-of select="count(key('tests-by-class', @type)[@result='Skip'])"/></xsl:attribute>

              <xsl:for-each select="key('tests-by-class', @type)">
                <xsl:sort select="@name"/>
                <test>
                  <xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
                  <xsl:attribute name="type"><xsl:value-of select="@type"/></xsl:attribute>
                  <xsl:attribute name="method"><xsl:value-of select="@method"/></xsl:attribute>
                  <xsl:attribute name="result"><xsl:value-of select="@result"/></xsl:attribute>
                  <xsl:attribute name="time"><xsl:value-of select="@time"/></xsl:attribute>
                  <xsl:if test="reason">
                    <reason>
                      <message><xsl:value-of select="reason/text()"/></message>
                    </reason>
                  </xsl:if>
                  <xsl:copy-of select="failure"/>
                  <xsl:copy-of select="traits"/>
                </test>
              </xsl:for-each>

            </class>
          </xsl:for-each>

        </assembly>
      </xsl:for-each>
    </assemblies>
  </xsl:template>
</xsl:stylesheet>