    public static class ReSharperGeneratedTransformationExecutor
    {
        public static int Main(string[] args)
        {
            string result = new $(PARAMETER_0)().TransformText();
            var encoding = Encoding.GetEncoding($(PARAMETER_1));
            File.WriteAllText(args[0], result, encoding);
            return 0;
        }
    }
