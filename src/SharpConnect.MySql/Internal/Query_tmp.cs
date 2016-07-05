﻿//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2013 Andrey Sidorov(sidorares @yandex.ru) and contributors
//MIT, 2015-2016, brezza92, EngineKit and contributors

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
using System.Collections.Generic;
using System.Threading;
namespace SharpConnect.MySql.Internal
{
    partial class Query
    {
        //temp here, to be deleted
        //void ParseResultSet()
        //{
        //    ResultSetHeaderPacket resultPacket = new ResultSetHeaderPacket();
        //    resultPacket.ParsePacket(_parser);
        //    this._tableHeader = new TableHeader();
        //    _tableHeader.TypeCast = typeCast;
        //    _tableHeader.NestTables = nestTables;
        //    _tableHeader.ConnConfig = _conn.config;
        //    bool protocol41 = _conn.IsProtocol41;
        //    while (_receiveBuffer[_parser.ReadPosition + 4] != EOF_CODE)
        //    {
        //        FieldPacket fieldPacket = ParseColumn();
        //        _tableHeader.AddField(fieldPacket);
        //    }

        //    EofPacket fieldEof = ParseEOF();
        //    //-----
        //    _lastRow = new RowDataPacket(_tableHeader);
        //}
        //OkPrepareStmtPacket ParsePrepareResponse()
        //{
        //    _receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];
        //    int receive = _conn.ReceiveData(_receiveBuffer);
        //    if (receive == 0)
        //    {
        //        return null;
        //    }
        //    //TODO: review err handling here
        //    //---------------------------------------------------
        //    _parser.LoadNewBuffer(_receiveBuffer, receive);
        //    OkPrepareStmtPacket okPreparePacket = new OkPrepareStmtPacket();
        //    switch (_receiveBuffer[4])
        //    {
        //        case ERROR_CODE:
        //            LoadError = new ErrPacket();
        //            LoadError.ParsePacket(_parser);
        //            okPreparePacket = null;
        //            break;
        //        case OK_CODE:
        //            okPreparePacket.ParsePacket(_parser);
        //            break;
        //    }
        //    return okPreparePacket;
        //}

        //EofPacket ParseEOF()
        //{
        //    EofPacket eofPacket = new EofPacket(_conn.IsProtocol41);//if temp[4]=0xfe then eof packet
        //    eofPacket.ParsePacketHeader(_parser);
        //    _receiveBuffer = CheckLimit(eofPacket.GetPacketLength(), _receiveBuffer, _receiveBuffer.Length);
        //    eofPacket.ParsePacket(_parser);
        //    LoadDataForNextPackets();
        //    return eofPacket;
        //}

        //FieldPacket ParseColumn()
        //{
        //    FieldPacket fieldPacket = new FieldPacket(_conn.IsProtocol41);
        //    fieldPacket.ParsePacketHeader(_parser);
        //    _receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), _receiveBuffer, _receiveBuffer.Length);
        //    fieldPacket.ParsePacket(_parser);
        //    LoadDataForNextPackets();
        //    return fieldPacket;
        //}

        //        byte[] CheckLimit(uint completePacketLength, byte[] buffer, int limit)
        //        {
        //            int availableBufferLength = (int)(buffer.Length - _parser.ReadPosition);
        //            if (availableBufferLength < completePacketLength)
        //            {
        //                int needMoreLength = (int)completePacketLength - availableBufferLength;
        //                int read_count = 0;
        //                int remainingBytes = 0;
        //                if (needMoreLength < limit)
        //                {
        //                    //use same buffer
        //                    //just shift buffer to left (at the begining, pos 0)
        //                    Buffer.BlockCopy(buffer, (int)_parser.ReadPosition, buffer, 0, availableBufferLength);
        //                    // _parser.Reset();
        //                    read_count = availableBufferLength;
        //                    remainingBytes = (int)completePacketLength - read_count;
        //                }
        //                else
        //                {
        //                    //we need to expand current buffer to a bigger one
        //                    var tmpBuffer = new byte[completePacketLength + 4];
        //                    Buffer.BlockCopy(buffer, (int)_parser.ReadPosition, tmpBuffer, 0, availableBufferLength);
        //                    // _parser.ResetStreamPos();
        //                    buffer = tmpBuffer;
        //                    read_count = availableBufferLength;
        //                    remainingBytes = (int)completePacketLength - read_count;
        //                }
        //                remainingBytes -= 12; //why?
        //                try
        //                {
        //                    int actualReceiveBytes = _conn.ReceiveData(buffer, read_count, remainingBytes);
        //                    read_count += actualReceiveBytes;
        //                    remainingBytes -= actualReceiveBytes;
        //                    int timeoutCountdown = 10000;
        //                    while (remainingBytes > 0)
        //                    {
        //                        // Console.WriteLine(remainingBytes.ToString());

        //                        int available = _conn.Available;
        //                        if (available > 0)
        //                        {
        //                            //read 
        //                            if (read_count + available <= completePacketLength)
        //                            {
        //                                actualReceiveBytes = _conn.ReceiveData(buffer, read_count, remainingBytes);
        //                            }
        //                            else
        //                            {
        //                                int x = (int)completePacketLength - read_count;
        //                                if (read_count + x > buffer.Length)
        //                                {
        //                                    int diff = (read_count + x) - buffer.Length;
        //                                }

        //                                actualReceiveBytes = _conn.ReceiveData(buffer, read_count, (int)completePacketLength - read_count);
        //                            }


        //                            read_count += actualReceiveBytes;
        //                            remainingBytes -= actualReceiveBytes;
        //                            timeoutCountdown = 10000;//reset, timeoutCountdown maybe < 10000 when socket receive faster than server send data
        //                        }
        //                        else
        //                        {
        //                            //TODO: review here !!!
        //                            Thread.Sleep(10);//sometime socket maybe receive faster than server send data
        //                            timeoutCountdown -= 10;
        //                            if (_conn.Available > 0)
        //                            {
        //                                continue;
        //                            }
        //                            if (timeoutCountdown <= 0)//sometime server maybe error
        //                            {
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                }

        //#if DEBUG
        //                var dbugView = new dbugBufferView(buffer, 11, read_count - 11);
        //                dbugView.viewIndex = dbugView.CheckNoDulpicateBytes();
        //#endif
        //                _parser.LoadNewBuffer(buffer, read_count);
        //            }
        //            return buffer;
        //        }




        //byte[] ReadPacket(byte[] recieveBuffer, PacketHeader header, PacketParser parser)
        //{
        //    if (!header.IsEmpty())
        //    {
        //        int recievedData = (int)(parser.CurrentInputLength - parser.ReadPosition);
        //        uint remainRecieve = header.ContentLength - (uint)recievedData;
        //        byte[] buffer = new byte[header.ContentLength];
        //        Buffer.BlockCopy(recieveBuffer, (int)parser.ReadPosition, buffer, 0, recievedData);
        //        if (remainRecieve > 0)
        //        {
        //            byte[] newRecieve = RecieveData((int)remainRecieve);
        //            byte[] end = new byte[100];//?
        //            Buffer.BlockCopy(newRecieve, newRecieve.Length - 101, end, 0, 100);
        //            parser.LoadNewBuffer(newRecieve, newRecieve.Length);
        //            Buffer.BlockCopy(newRecieve, 0, buffer, recievedData, newRecieve.Length);
        //        }
        //        while (header.ContentLength >= MAX_PACKET_LENGTH)
        //        {
        //            byte[] temp = RecieveData(4);//for header
        //            parser.Reset();
        //            parser.LoadNewBuffer(temp, temp.Length);
        //            header = parser.ParsePacketHeader();
        //            parser.Reset();//reset header
        //            temp = (byte[])buffer.Clone();//prevent copy by reference
        //            buffer = new byte[buffer.Length + header.ContentLength];
        //            Buffer.BlockCopy(temp, 0, buffer, 0, temp.Length);
        //            int lastPosition = temp.Length;
        //            temp = RecieveData((int)header.ContentLength);
        //            byte[] end = new byte[100];
        //            Buffer.BlockCopy(temp, temp.Length - 101, end, 0, 100);
        //            parser.LoadNewBuffer(temp, temp.Length);
        //            Buffer.BlockCopy(temp, 0, buffer, lastPosition, temp.Length);
        //        }
        //        parser.LoadNewBuffer(buffer, buffer.Length);
        //        return buffer;
        //    }
        //    else
        //    {
        //        throw new Exception("Expected non empty header");
        //    }
        //}


        //byte[] RecieveData(int n)
        //{
        //    if (n > 0)
        //    {
        //        byte[] recieved = new byte[n];
        //        int actualRecieve = _conn.ReceiveData(recieved);
        //        int socketEmptyCount = 0;
        //        while (actualRecieve < n)
        //        {
        //            if (_conn.Available > 0)
        //            {
        //                actualRecieve += _conn.ReceiveData(recieved, actualRecieve, n - actualRecieve);
        //                socketEmptyCount = 0;
        //            }
        //            else
        //            {
        //                if (socketEmptyCount >= 1000)
        //                {
        //                    throw new Exception("Socket Not Available!!");
        //                }
        //                else
        //                {
        //                    socketEmptyCount++;
        //                    Thread.Sleep(10);
        //                }
        //            }
        //        }
        //        return recieved;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
        //void LoadDataForNextPackets()
        //{
        //    byte[] buffer = _receiveBuffer;
        //    //todo: check memory mx again
        //    int remainLength = (int)(_parser.CurrentInputLength - _parser.ReadPosition);
        //    if (remainLength < 5)//5 bytes --> 4 bytes from header and 1 byte for find packet type
        //    {
        //        //byte[] remainBuff = new byte[remainLength];
        //        if (remainLength > 0)
        //        {
        //            //shift current data in buffer to the left most
        //            Buffer.BlockCopy(buffer, (int)_parser.ReadPosition, buffer, 0, remainLength);
        //        }

        //        int bufferRemain = buffer.Length - remainLength;
        //        int available = _conn.Available;// socket.Available;
        //        if (available == 0)//it finished
        //        {
        //            return;
        //        }
        //        int expectedReceive = (available < bufferRemain ? available : bufferRemain);
        //        int realReceive = _conn.ReceiveData(buffer, remainLength, expectedReceive);
        //        int newBufferLength = remainLength + realReceive;//sometime realReceive != expectedReceive
        //        _parser.LoadNewBuffer(buffer, newBufferLength);
        //        dbugConsole.WriteLine("CheckBeforeParseHeader : LoadNewBuffer");
        //    }
        //}
        //const int DEFAULT_BUFFER_SIZE = 512;
        //const byte ERROR_CODE = 255;
        //const byte EOF_CODE = 0xfe;
        //const byte OK_CODE = 0;
        //const int MAX_PACKET_LENGTH = (1 << 24) - 1;//(int)Math.Pow(2, 24) - 1; 
        //byte[] _receiveBuffer;
        // PacketParser _parser;
    }
}