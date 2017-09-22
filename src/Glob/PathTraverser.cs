using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System;

namespace Glob
{
    static class PathTraverser
    {        
        static bool GetSimplePattern(this Segment segment, out string pattern)
        {
            switch (segment)
            {
                case Root root:
                    pattern = root.Text;
                    return true;
                case DirectoryWildcard wildcard:
                    pattern = "*";
                    return true;
                case DirectorySegment directory:
                    return directory.GetSimplePattern(out pattern);

                default:
                    pattern = null;
                    return false;
            }
        }

        static bool GetSimplePattern(this DirectorySegment segment, out string pattern)
        {
            var sb = new StringBuilder();

            foreach(var sub in segment.SubSegments)
            {
                switch (sub) {
                    case StringWildcard _:
                        sb.Append("*");
                        break;

                    case CharacterWildcard _:
                        sb.Append("?");
                        break;

                    case Identifier ident:
                        sb.Append(ident.Value);
                        break;

                    case LiteralSet _:
                    case CharacterSet _:
                    default:
                        pattern = null;
                        return false;
                }
            }

            pattern = sb.ToString();
            return true;
        }

        static IEnumerable<GlobNode> EnumerateGlobNode(GlobNode node)
        {
            IEnumerable<GlobNode> Children(IEnumerable<GlobNode> nodes) =>
                from segment in nodes
                from item in EnumerateGlobNode(segment)
                select item;
            
            switch (node)
            {
                case Tree tree:
                {
                    yield return tree;                        

                    foreach (var item in Children(tree.Segments))
                        yield return item;

                    break;
                }

                case DirectorySegment segment:
                {
                    yield return segment;

                    foreach (var item in Children(segment.SubSegments))
                        yield return item;

                    break;
                }

                default:
                    yield return node;
                    break;
            }
        }

        //static IEnumerable<FileSystemInfo> EnumerateDirectories(GlobNode node, string path, int index = 0)
        //{
        //    switch(node)
        //    {
        //        case Root root: 
        //            if(path.Name == root.Text)
        //            {
        //                path.EnumerateFileSystemInfos
        //            }
        //        case Tree t:
        //            switch(t.Segments.Count)
        //            {
        //                case 0:
        //                    yield break;

        //                case 1:
        //                    foreach (var item in EnumerateDirectories(t.Segments.First(), path))
        //                        yield return item;
        //                    yield break;
        //                default:
        //            }
        //            break;
        //    }

        //}
        

        public static IEnumerable<FileSystemInfo> Glob(this FileSystemInfo di, string pattern)
        {
            var parser = new Parser(pattern);
            var segments = parser.ParseTree().Segments.ToLst();

            var pathSegments = di.FullName.Split(new[] { Path.DirectorySeparatorChar }).ToLst();
            return VerifyPath(di, pathSegments, segments);
        }

        private static IEnumerable<FileSystemInfo> VerifyPath(FileSystemInfo info, Lst<string> pathSegments, Lst<Segment> segments)
        {
            switch (pathSegments)
            {
                case Nil<string> _:
                    foreach(var item in CheckSubPaths(info, segments))
                        yield return item;
                    break;

                case Cons<string> lst:
                    var (head, tail) = lst;

                    foreach (var item in VerifyOnePath(info, head, tail, segments))
                        yield return item;

                    break;
            }
        }

        private static IEnumerable<FileSystemInfo> CheckSubPaths(FileSystemInfo info, Lst<Segment> segments)
        {
            if(info is FileInfo)
            {
                yield return info;
            }
            else if(info is DirectoryInfo di)
            {
                //di.EnumerateFileSystemInfos()
            }

        }

        private static IEnumerable<FileSystemInfo> VerifyOnePath(FileSystemInfo info, string head, Lst<string> tail, Lst<Segment> segments)
        {
            switch (segments)
            {
                case Nil<Segment> _: // we have a path to match but nothing to match against so we are done.
                    yield break;

                case Cons<Segment> cons:
                    var (shead, stail) = cons;

                    switch (shead)
                    {
                        case DirectoryWildcard _:
                            // return all consuming the wildcard
                            foreach (var item in VerifyPath(info, tail, stail))
                                yield return item;

                            // return all while not consuming the wildcard
                            foreach (var item in VerifyPath(info, tail, cons))
                                yield return item;

                            break;
                        case Root root when head == root.Text:
                            foreach (var item in VerifyPath(info, tail, stail))
                                yield return item;
                            break;

                        case DirectorySegment dir when dir.MatchesSegment(head):
                            foreach (var item in VerifyPath(info, tail, stail))
                                yield return item;
                            break;
                    }

                    break;
            }
        }
    }
}
