using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TmxReadAndWrite.IO;
internal static class FileManager
{
    static Dictionary<Type, XmlSerializer> mXmlSerializers = new Dictionary<Type, XmlSerializer>();

    public static void XmlSerialize<T>(T objectToSerialize, out string stringToSerializeTo)
    {
        MemoryStream memoryStream = new MemoryStream();

        XmlSerializer serializer = GetXmlSerializer(typeof(T));

        serializer.Serialize(memoryStream, objectToSerialize);


#if MONODROID

            byte[] asBytes = memoryStream.ToArray();

            stringToSerializeTo = System.Text.Encoding.UTF8.GetString(asBytes, 0, asBytes.Length);
#else

        stringToSerializeTo = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
#endif

    }


    public static void XmlSerialize<T>(T objectToSerialize, string fileName)
    {

        XmlSerialize(typeof(T), objectToSerialize, fileName);
    }


    public static void XmlSerialize(Type type, object objectToSerialize, string fileName)
    {
        //if (FileManager.IsRelative(fileName))
        //    fileName = FileManager.RelativeDirectory + fileName;

#if USE_ISOLATED_STORAGE
            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }

            fileName = GetIsolatedStorageFileName(fileName);

#endif

#if IOS
            // The "AllOtherPlatforms" method worked on iOS, but
            // only once. After that, files could not be written to - 
            // I'd get an unauthorized exception.
            // 
            XmlSerializeiOS(type, objectToSerialize, fileName);
#else

        XmlSerializeAllOtherPlatforms(type, objectToSerialize, fileName);
#endif
    }


    private static void XmlSerializeAllOtherPlatforms(Type type, object objectToSerialize, string fileName)
    {
        string serializedText;
        FileManager.XmlSerialize(type, objectToSerialize, out serializedText);

        FileManager.SaveText(serializedText, fileName);
    }


    public static void XmlSerialize(Type type, object objectToSerialize, out string stringToSerializeTo)
    {
        using (var memoryStream = new MemoryStream())
        {
            XmlSerializer serializer = GetXmlSerializer(type);
            Encoding utf8EncodingWithNoByteOrderMark = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            XmlTextWriter xtw = new XmlTextWriter(memoryStream, utf8EncodingWithNoByteOrderMark);
            xtw.Indentation = 2;
            xtw.Formatting = Formatting.Indented;
            serializer.Serialize(xtw, objectToSerialize);


#if MONOGAME
			    byte[] asBytes = memoryStream.ToArray();
			    stringToSerializeTo = System.Text.Encoding.UTF8.GetString(asBytes, 0, asBytes.Length);
#else
            stringToSerializeTo = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
#endif
        }

    }


    public static void SaveText(string stringToSave, string fileName)
    {
        SaveText(stringToSave, fileName, System.Text.Encoding.UTF8);
    }


    private static void SaveText(string stringToSave, string fileName, System.Text.Encoding encoding)
    {
        // encoding is currently unused
        fileName = fileName.Replace("/", "\\");

        ////////////Early Out///////////////////////
        // Glue is Desktop_GL as of October 26, 2024, so we should prob do this:
#if WINDOW || DESKTOP_GL
            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
                !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }

            // Note: On Windows, WrietAllText causes 
            // 2 file changes to be raised if the file already exists.
            // This makes Glue always reload the .glux
            // on any file change. This is slow, inconvenient,
            // and can introduce bugs.
            // Therefore, we have to delete the file first to prevent
            // two file changes:

            if(System.IO.File.Exists(fileName))
            {
                System.IO.File.Delete(fileName);
            }

            System.IO.File.WriteAllText(fileName, stringToSave);
            return;
#endif
        ////////////End Early Out///////////////////////////






        StreamWriter writer = null;

#if MONOGAME && !DESKTOP_GL && !STANDARD


            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }

            fileName = FileManager.GetIsolatedStorageFileName(fileName);

#if IOS || ANDROID
            throw new NotImplementedException();
#else
            IsolatedStorageFileStream isfs = null;

            isfs = new IsolatedStorageFileStream(
                fileName, FileMode.Create, mIsolatedStorageFile);

            writer = new StreamWriter(isfs);
#endif

#else
        //if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
        //    !Directory.Exists(FileManager.GetDirectory(fileName)))
        //{
        //    Directory.CreateDirectory(FileManager.GetDirectory(fileName));
        //}


        FileInfo fileInfo = new FileInfo(fileName);
        // We used to first delete the file to try to prevent the
        // OS from reporting 2 file accesses.  But I don't think this
        // solved the problem *and* it has the nasty side effect of possibly
        // deleting the entire file , but not being able to save it if there is
        // some weird access issue.  This would result in Glue deleting some files
        // like the user's Game1 or plugins not properly saving files 
        //if (System.IO.File.Exists(fileName))
        //{
        //    System.IO.File.Delete(fileName);
        //}
        writer = fileInfo.CreateText();



#endif

        using (writer)
        {
            writer.Write(stringToSave);

            Close(writer);
        }

#if MONODROID
            isfs.Close();
            isfs.Dispose();
#endif
    }

    private static void Close(StreamWriter writer)
    {
        writer.Close();
    }

    public static XmlSerializer GetXmlSerializer(Type type)
    {
        if (mXmlSerializers.ContainsKey(type))
        {
            return mXmlSerializers[type];
        }
        else
        {

            // For info on this block, see:
            // http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
#if DEBUG
            XmlSerializer newSerializer = XmlSerializer.FromTypes(new[] { type })[0];
#else
                XmlSerializer newSerializer = null;
                newSerializer = new XmlSerializer(type);
#endif
            mXmlSerializers.Add(type, newSerializer);
            return newSerializer;
        }
    }

    public static T XmlDeserialize<T>(string fileName)
    {
        T objectToReturn = default(T);


        //if (FileManager.IsRelative(fileName))
        //    fileName = FileManager.RelativeDirectory + fileName;




        //ThrowExceptionIfFileDoesntExist(fileName);


        //if (IsMobile)
        //{
        //    // Mobile platforms don't like ./ at the start of the file name, but that's what we use to identify an absolute path
        //    fileName = TryRemoveLeadingDotSlash(fileName);
        //}


        using (Stream stream = GetStreamForFile(fileName))
        {
            try
            {
                objectToReturn = XmlDeserializeFromStream<T>(stream);
            }
            catch (Exception e)
            {
                throw new IOException("Could not deserialize the XML file"
                    + Environment.NewLine + fileName, e);
            }
            stream.Close();
        }

        return objectToReturn;
    }


    public static Stream GetStreamForFile(string fileName)
    {

        try
        {

#if ANDROID || IOS
                fileName = TryRemoveLeadingDotSlash(fileName);
			    return Microsoft.Xna.Framework.TitleContainer.OpenStream(fileName);
#else
            //if (CustomGetStreamFromFile != null)
            //{
            //    return CustomGetStreamFromFile(fileName);
            //}
            //else
            {
                //if (IsRelative(fileName))
                //{
                //    fileName = FileManager.MakeAbsolute(fileName);
                //}
                return System.IO.File.OpenRead(fileName);
            }
#endif
        }
        catch (Exception e)
        {
            throw new IOException("Could not get the stream for the file " + fileName, e);
        }
    }


    public static T XmlDeserializeFromStream<T>(Stream stream)
    {
        Type type = typeof(T);

        XmlSerializer serializer = GetXmlSerializer(type);

        T objectToReturn = (T)serializer.Deserialize(stream);

        return objectToReturn;
    }

}
