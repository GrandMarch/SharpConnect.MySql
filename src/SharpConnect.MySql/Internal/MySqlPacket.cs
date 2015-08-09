﻿//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2015 brezza27, EngineKit and contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Text;

namespace MySqlPacket
{

    abstract class Packet
    {
        protected PacketHeader header;

        public abstract void ParsePacket(PacketParser parser);

        public virtual void ParsePacketHeader(PacketParser parser)
        {
            if (header.IsEmpty())
            {
                header = parser.ParsePacketHeader();
            }
        }

        public virtual uint GetPacketLength()
        {
            return header.Length;
        }

        public abstract void WritePacket(PacketWriter writer);
    }

    class ClientAuthenticationPacket : Packet
    {
        public uint clientFlags;
        public uint maxPacketSize;
        public byte charsetNumber;
        public byte[] filler;
        public string user;
        public byte[] scrambleBuff;
        public string database;
        public bool protocol41;

        public ClientAuthenticationPacket()
        {
            SetDefaultValues();
        }

        void SetDefaultValues()
        {
            clientFlags = 455631;
            maxPacketSize = 0;
            charsetNumber = 33;
            filler = new byte[23];
            user = "";
            scrambleBuff = new byte[20];
            database = "";
            protocol41 = true;
        }

        public void SetValues(string username, byte[] scrambleBuff, string databaseName, bool protocol41)
        {
            clientFlags = 455631;
            maxPacketSize = 0;
            charsetNumber = 33;
            filler = new byte[23];
            this.user = username;
            this.scrambleBuff = scrambleBuff;
            this.database = databaseName;
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            if (this.protocol41)
            {
                this.clientFlags = parser.ParseUnsignedNumber(4);
                this.maxPacketSize = parser.ParseUnsignedNumber(4);
                this.charsetNumber = parser.ParseByte();
                this.filler = parser.ParseFiller(23);
                this.user = parser.ParseNullTerminatedString();
                this.scrambleBuff = parser.ParseLengthCodedBuffer();
                this.database = parser.ParseNullTerminatedString();
            }
            else
            {
                this.clientFlags = parser.ParseUnsignedNumber(2);
                this.maxPacketSize = parser.ParseUnsignedNumber(3);
                this.user = parser.ParseNullTerminatedString();
                this.scrambleBuff = parser.ParseBuffer(8);
                this.database = parser.ParseLengthCodedString();
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();//allocate header
            if (protocol41)
            {
                writer.WriteUnsignedNumber(4, this.clientFlags);
                writer.WriteUnsignedNumber(4, this.maxPacketSize);
                writer.WriteUnsignedNumber(1, this.charsetNumber);
                writer.WriteFiller(23);
                writer.WriteNullTerminatedString(this.user);
                writer.WriteLengthCodedBuffer(this.scrambleBuff);
                writer.WriteNullTerminatedString(this.database);
            }
            else
            {
                writer.WriteUnsignedNumber(2, this.clientFlags);
                writer.WriteUnsignedNumber(3, this.maxPacketSize);
                writer.WriteNullTerminatedString(this.user);
                writer.WriteBuffer(this.scrambleBuff);
                if (this.database != null && this.database.Length > 0)
                {
                    writer.WriteFiller(1);
                    writer.WriteBuffer(Encoding.ASCII.GetBytes(this.database));
                }
            }
            header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
        }

    }

    class ComQueryPacket : Packet
    {
        uint command = 0x03;
        string sql;

        public ComQueryPacket(string sql)
        {
            this.sql = sql;
        }

        public override void ParsePacket(PacketParser parser)
        {
            //parser = new PacketParser(stream);
            ParsePacketHeader(parser);
            this.command = parser.ParseUnsignedNumber(1);
            this.sql = parser.ParsePacketTerminatedString();
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();

            writer.WriteByte((byte)command);
            writer.WriteString(this.sql);

            header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
        }
    }

    class ComQuitPacket : Packet
    {
        byte command = 0x01;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            this.command = parser.ParseByte();
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();
            writer.WriteUnsignedNumber(1, this.command);
            header = new PacketHeader((uint)writer.Length, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
        }
    }

    class EofPacket : Packet
    {
        public byte fieldCount;
        public uint warningCount;
        public uint serverStatus;
        public bool protocol41;

        public EofPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            this.fieldCount = parser.ParseByte();
            if (this.protocol41)
            {
                this.warningCount = parser.ParseUnsignedNumber(2);
                this.serverStatus = parser.ParseUnsignedNumber(2);
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();//allocate packet header

            writer.WriteUnsignedNumber(1, 0xfe);
            if (this.protocol41)
            {
                writer.WriteUnsignedNumber(2, this.warningCount);
                writer.WriteUnsignedNumber(2, this.serverStatus);
            }

            header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);//write packet header
        }
    }

    class ErrPacket : Packet
    {
        byte fieldCount;
        uint errno;
        char sqlStateMarker;
        string sqlState;
        public string message;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);

            fieldCount = parser.ParseByte();
            errno = parser.ParseUnsignedNumber(2);

            if (parser.Peak() == 0x23)
            {
                sqlStateMarker = parser.ParseChar();
                sqlState = parser.ParseString(5);
            }

            message = parser.ParsePacketTerminatedString();
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class FieldPacket : Packet
    {
        public string catalog;
        public string db;
        public string table;
        public string orgTable;
        public string name;
        public string orgName;
        public uint charsetNr;
        public uint length;
        public int type;
        public uint flags;
        public byte decimals;
        public byte[] filler;
        public bool zeroFill;
        public string strDefault;
        public bool protocol41;

        public FieldPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            if (this.protocol41)
            {
                this.catalog = parser.ParseLengthCodedString();
                this.db = parser.ParseLengthCodedString();
                this.table = parser.ParseLengthCodedString();
                this.orgTable = parser.ParseLengthCodedString();
                this.name = parser.ParseLengthCodedString();
                this.orgName = parser.ParseLengthCodedString();

                if (parser.ParseLengthCodedNumber() != 0x0c)
                {
                    //var err  = new TypeError('Received invalid field length');
                    //err.code = 'PARSER_INVALID_FIELD_LENGTH';
                    //throw err;
                    throw new Exception("Received invalid field length");
                }

                this.charsetNr = parser.ParseUnsignedNumber(2);
                this.length = parser.ParseUnsignedNumber(4);
                this.type = parser.ParseByte();
                this.flags = parser.ParseUnsignedNumber(2);
                this.decimals = parser.ParseByte();

                this.filler = parser.ParseBuffer(2);
                if (filler[0] != 0x0 || filler[1] != 0x0)
                {
                    //var err  = new TypeError('Received invalid filler');
                    //err.code = 'PARSER_INVALID_FILLER';
                    //throw err;
                    throw new Exception("Received invalid filler");
                }

                // parsed flags
                //this.zeroFill = (this.flags & 0x0040 ? true : false);
                this.zeroFill = ((this.flags & 0x0040) == 0x0040 ? true : false);

                //    if (parser.reachedPacketEnd()) {
                //      return;
                //    }
                if (parser.ReachedPacketEnd())
                {
                    return;
                }
                this.strDefault = parser.ParseLengthCodedString();
            }
            else
            {
                this.table = parser.ParseLengthCodedString();
                this.name = parser.ParseLengthCodedString();
                this.length = parser.ParseUnsignedNumber(parser.ParseByte());
                this.type = (int)parser.ParseUnsignedNumber(parser.ParseByte());
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return name;
        }
    }

    class HandshakePacket : Packet
    {
        public uint protocolVersion;
        public string serverVertion;
        public uint threadId;
        public byte[] scrambleBuff1;
        public byte filler1;
        public uint serverCapabilities1;
        public byte serverLanguage;
        public uint serverStatus;
        public bool protocol41;
        public uint serverCapabilities2;
        public byte scrambleLength;
        public byte[] filler2;
        public byte[] scrambleBuff2;
        public byte filler3;
        public string pluginData;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);

            protocolVersion = parser.ParseUnsignedNumber(1);
            serverVertion = parser.ParseNullTerminatedString();
            threadId = parser.ParseUnsignedNumber(4);
            scrambleBuff1 = parser.ParseBuffer(8);
            filler1 = parser.ParseByte();
            serverCapabilities1 = parser.ParseUnsignedNumber(2);
            serverLanguage = parser.ParseByte();
            serverStatus = parser.ParseUnsignedNumber(2);

            protocol41 = (serverCapabilities1 & (1 << 9)) > 0;
            if (protocol41)
            {
                serverCapabilities2 = parser.ParseUnsignedNumber(2);
                scrambleLength = parser.ParseByte();
                filler2 = parser.ParseBuffer(10);

                scrambleBuff2 = parser.ParseBuffer(12);
                filler3 = parser.ParseByte();
            }
            else
            {
                filler2 = parser.ParseBuffer(13);
            }

            if (parser.Position == parser.Length)
            {
                return;
            }

            pluginData = parser.ParsePacketTerminatedString();
            var last = pluginData.Length - 1;
            if (pluginData[last] == '\0')
            {
                pluginData = pluginData.Substring(0, last);
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            //writer.writeUnsignedNumber(1, this.protocolVersion);
            //writer.writeNullTerminatedString(this.serverVersion);
            //writer.writeUnsignedNumber(4, this.threadId);
            //writer.writeBuffer(this.scrambleBuff1);
            //writer.writeFiller(1);
            //writer.writeUnsignedNumber(2, this.serverCapabilities1);
            //writer.writeUnsignedNumber(1, this.serverLanguage);
            //writer.writeUnsignedNumber(2, this.serverStatus);
            //if (this.protocol41) {
            //  writer.writeUnsignedNumber(2, this.serverCapabilities2);
            //  writer.writeUnsignedNumber(1, this.scrambleLength);
            //  writer.writeFiller(10);
            //}
            //writer.writeNullTerminatedBuffer(this.scrambleBuff2);

            //if (this.pluginData !== undefined) {
            //  writer.writeNullTerminatedString(this.pluginData);
            //}
        }
    }

    class OkPacket : Packet
    {
        uint fieldCount;
        public uint affectedRows;
        public uint insertId;
        uint serverStatus;
        uint warningCount;
        string message;
        bool protocol41;

        public OkPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);

            fieldCount = parser.ParseUnsignedNumber(1);
            affectedRows = parser.ParseLengthCodedNumber();
            insertId = parser.ParseLengthCodedNumber();

            //this.fieldCount = parser.parseUnsignedNumber(1);
            //this.affectedRows = parser.parseLengthCodedNumber();
            //this.insertId = parser.parseLengthCodedNumber();
            //if (this.protocol41)
            //{
            //    this.serverStatus = parser.parseUnsignedNumber(2);
            //    this.warningCount = parser.parseUnsignedNumber(2);
            //}
            //this.message = parser.parsePacketTerminatedString();
            //this.changedRows = 0;
            if (protocol41)
            {
                serverStatus = parser.ParseUnsignedNumber(2);
                warningCount = parser.ParseUnsignedNumber(2);
            }
            message = parser.ParsePacketTerminatedString();
            //var m = this.message.match(/\schanged:\s * (\d +) / i);

            //if (m !== null)
            //{
            //    this.changedRows = parseInt(m[1], 10);
            //}
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class ResultSetHeaderPacket : Packet
    {
        long fieldCount;
        uint extraNumber;
        string extraStr;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            this.fieldCount = parser.ParseLengthCodedNumber();

            if (parser.ReachedPacketEnd())
                return;

            if (this.fieldCount == 0)
            {
                extraStr = parser.ParsePacketTerminatedString();
            }
            else
            {
                extraNumber = parser.ParseLengthCodedNumber();
                extraStr = null;
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();
            //writer.WriteLengthCodedNumber(this.fieldCount);

            //if (this.extra !== undefined) {
            //  writer.WriteLengthCodedNumber(this.extra);
            //}
        }
    }

    class RowDataPacket : Packet
    {


        MyStructData[] myDataList;
        TableHeader tableHeader;
        const long IEEE_754_BINARY_64_PRECISION = (long)1 << 53;

        public RowDataPacket(TableHeader tableHeader)
        {
            this.tableHeader = tableHeader;
            myDataList = new MyStructData[tableHeader.ColumnCount];

        }
        public void ReuseSlots()
        {
            //this is reuseable row packet
            this.header = PacketHeader.Empty;
            Array.Clear(myDataList, 0, myDataList.Length);

        }
        public override void ParsePacket(PacketParser parser)
        {
            //function parse(parser, fieldPackets, typeCast, nestTables, connection) {
            //  var self = this;
            //  var next = function () {
            //    return self._typeCast(fieldPacket, parser, connection.config.timezone, connection.config.supportBigNumbers, connection.config.bigNumberStrings, connection.config.dateStrings);
            //  };

            //  for (var i = 0; i < fieldPackets.length; i++) {
            //    var fieldPacket = fieldPackets[i];
            //    var value;
            ParsePacketHeader(parser);
            var fieldInfos = tableHeader.GetFields();
            int j = tableHeader.ColumnCount;
            bool typeCast = tableHeader.TypeCast;
            bool nestTables = tableHeader.NestTables;

            for (int i = 0; i < j; i++)
            {

                MyStructData value;
                if (typeCast)
                {
                    ConnectionConfig config = tableHeader.ConnConfig;
                    value = TypeCast(parser,
                        fieldInfos[i],
                        config.timezone,
                        config.supportBigNumbers,
                        config.bigNumberStrings,
                        config.dateStrings);
                }
                else if (fieldInfos[i].charsetNr == (int)CharSets.BINARY)
                {
                    value = new MyStructData();
                    value.myBuffer = parser.ParseLengthCodedBuffer();
                    value.type = (Types)fieldInfos[i].type;
                }
                else
                {
                    value = new MyStructData();
                    value.myString = parser.ParseLengthCodedString();
                    value.type = (Types)fieldInfos[i].type;
                }
                //    if (typeof typeCast == "function") {
                //      value = typeCast.apply(connection, [ new Field({ packet: fieldPacket, parser: parser }), next ]);
                //    } else {
                //      value = (typeCast)
                //        ? this._typeCast(fieldPacket, parser, connection.config.timezone, connection.config.supportBigNumbers, connection.config.bigNumberStrings, connection.config.dateStrings)
                //        : ( (fieldPacket.charsetNr === Charsets.BINARY)
                //          ? parser.parseLengthCodedBuffer()
                //          : parser.parseLengthCodedString() );
                //    }
                if (nestTables)
                {
                    //      this[fieldPacket.table] = this[fieldPacket.table] || {};
                    //      this[fieldPacket.table][fieldPacket.name] = value;
                }
                else
                {
                    //      this[fieldPacket.name] = value;
                    myDataList[i] = value;
                }
                //    if (typeof nestTables == "string" && nestTables.length) {
                //      this[fieldPacket.table + nestTables + fieldPacket.name] = value;
                //    } else if (nestTables) {
                //      this[fieldPacket.table] = this[fieldPacket.table] || {};
                //      this[fieldPacket.table][fieldPacket.name] = value;
                //    } else {
                //      this[fieldPacket.name] = value;
                //    }
                //  }
                //}
            }
        }

        static MyStructData TypeCast(PacketParser parser, FieldPacket fieldPacket, string timezone, bool supportBigNumbers, bool bigNumberStrings, bool dateStrings)
        {
            //var numberString;
            string numberString;
            Types type = (Types)fieldPacket.type;
            MyStructData data = new MyStructData();
            switch (type)
            {
                case Types.TIMESTAMP:
                case Types.DATE:
                case Types.DATETIME:
                case Types.NEWDATE:
                    StringBuilder strBuilder = new StringBuilder();
                    string dateString = parser.ParseLengthCodedString();
                    if (dateStrings)
                    {
                        //return new FieldData<string>(type, dateString);
                        data.myString = dateString;
                        data.type = type;
                        return data;
                    }

                    if (dateString == null)
                    {
                        data.type = Types.NULL;
                        return data;
                    }

                    //    var originalString = dateString;
                    //    if (field.type === Types.DATE) {
                    //      dateString += ' 00:00:00';
                    //    }
                    strBuilder.Append(dateString);
                    string originalString = dateString;
                    if (fieldPacket.type == (int)Types.DATE)
                    {
                        strBuilder.Append(" 00:00:00");
                    }
                    //    if (timeZone !== 'local') {
                    //      dateString += ' ' + timeZone;
                    //    }
                    if (!timezone.Equals("local"))
                    {
                        strBuilder.Append(' ' + timezone);
                    }
                    //var dt;
                    //    dt = new Date(dateString);
                    //    if (isNaN(dt.getTime())) {
                    //      return originalString;
                    //    }
                    DateTime dt = DateTime.Parse(strBuilder.ToString());
                    //return new FieldData<DateTime>(type, dt);
                    data.myDateTime = dt;
                    data.type = type;
                    return data;
                case Types.TINY:
                case Types.SHORT:
                case Types.LONG:
                case Types.INT24:
                case Types.YEAR:
                    numberString = parser.ParseLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0') || numberString.Length == 0)
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else
                    {
                        //return new FieldData<int>(type, Convert.ToInt32(numberString));
                        data.myInt32 = Convert.ToInt32(numberString);
                        data.type = type;
                    }
                    return data;
                case Types.FLOAT:
                case Types.DOUBLE:
                    numberString = parser.ParseLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0'))
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else
                    {
                        //return new FieldData<double>(type, Convert.ToDouble(numberString));
                        data.myDouble = Convert.ToDouble(numberString);
                        data.type = type;
                    }
                    return data;
                //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                //      ? numberString : Number(numberString);
                case Types.NEWDECIMAL:
                case Types.LONGLONG:
                    //    numberString = parser.parseLengthCodedString();
                    //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                    //      ? numberString
                    //      : ((supportBigNumbers && (bigNumberStrings || (Number(numberString) > IEEE_754_BINARY_64_PRECISION)))
                    //        ? numberString
                    //        : Number(numberString));
                    numberString = parser.ParseLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0'))
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else if (supportBigNumbers && (bigNumberStrings || (Convert.ToInt64(numberString) > IEEE_754_BINARY_64_PRECISION)))
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = type;
                    }
                    else if (type == Types.LONGLONG)
                    {
                        //return new FieldData<long>(type, Convert.ToInt64(numberString));
                        data.myLong = Convert.ToInt64(numberString);
                        data.type = type;
                    }
                    else//decimal
                    {
                        data.myDecimal = Convert.ToDecimal(numberString);
                        data.type = type;
                    }
                    return data;
                case Types.BIT:
                    //return new FieldData<byte[]>(type, parser.ParseLengthCodedBuffer());
                    data.myBuffer = parser.ParseLengthCodedBuffer();
                    data.type = type;
                    return data;
                //    return parser.parseLengthCodedBuffer();
                case Types.STRING:
                case Types.VAR_STRING:
                case Types.TINY_BLOB:
                case Types.MEDIUM_BLOB:
                case Types.LONG_BLOB:
                case Types.BLOB:
                    if (fieldPacket.charsetNr == (int)CharSets.BINARY)
                    {
                        //return new FieldData<byte[]>(type, parser.ParseNullTerminatedBuffer());
                        data.myBuffer = parser.ParseLengthCodedBuffer();
                        data.type = type;
                    }
                    else
                    {
                        //return new FieldData<string>(type, parser.ParseLengthCodedString());
                        data.myString = parser.ParseLengthCodedString();
                        data.type = type;
                    }
                    return data;
                //    return (field.charsetNr === Charsets.BINARY)
                //      ? parser.parseLengthCodedBuffer()
                //      : parser.parseLengthCodedString();
                case Types.GEOMETRY:
                    //    return parser.parseGeometryValue();
                    return data;
                default:
                    //return new FieldData<string>(type, parser.ParseLengthCodedString());
                    data.myString = parser.ParseLengthCodedString();
                    data.type = type;
                    return data;
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            int count = myDataList.Length;
            for (int i = 0; i < count; i++)
            {
                strBuilder.Append(myDataList[i].ToString());
                if (i < count - 1)
                {
                    strBuilder.Append(", ");
                }
            }
            return strBuilder.ToString();
        }

        //-----------------------------------------------------
        public MyStructData GetDataInField(int fieldIndex)
        {
            if (fieldIndex < tableHeader.ColumnCount)
            {
                return myDataList[fieldIndex];
            }
            else
            {
                MyStructData data = new MyStructData();
                data.myString = "index out of range!";
                data.type = Types.STRING;
                return data;
            }
        }
        public MyStructData GetDataInField(string fieldName)
        {
            int index = tableHeader.GetFieldIndex(fieldName);
            if (index < 0)
            {
                MyStructData data = new MyStructData();
                data.myString = "Not found field name '" + fieldName + "'";
                data.type = Types.STRING;
                return data;
            }
            else
            {
                return myDataList[index];
            }

        }
    }
}