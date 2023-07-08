using System;

using OpenTK.Graphics.OpenGL;

namespace PRR.OpenGL;

public class Shader : IDisposable {
    private int _handle;

    public Shader(string vertexSource, string fragmentSource) {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

        GL.ShaderSource(vertexShader, vertexSource);
        GL.ShaderSource(fragmentShader, fragmentSource);

        GL.CompileShader(vertexShader);
        GL.CompileShader(fragmentShader);

        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexSuccess);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentSuccess);
        if(vertexSuccess == 0) {
            string infoLog = GL.GetShaderInfoLog(vertexShader);
            Console.WriteLine(infoLog);
        }
        if(fragmentSuccess == 0) {
            string infoLog = GL.GetShaderInfoLog(fragmentShader);
            Console.WriteLine(infoLog);
        }

        if(vertexSuccess == 0 || fragmentSuccess == 0) {
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            return;
        }

        _handle = GL.CreateProgram();

        GL.AttachShader(_handle, vertexShader);
        GL.AttachShader(_handle, fragmentShader);

        GL.LinkProgram(_handle);

        GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out int success);
        if(success == 0) {
            string infoLog = GL.GetProgramInfoLog(_handle);
            Console.WriteLine(infoLog);
            GL.DetachShader(_handle, vertexShader);
            GL.DetachShader(_handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteProgram(_handle);
            return;
        }

        GL.DetachShader(_handle, vertexShader);
        GL.DetachShader(_handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Use() => GL.UseProgram(_handle);

    public int GetUniformLocation(string name) => GL.GetUniformLocation(_handle, name);

    public void Dispose() {
        if(_handle != 0) {
            GL.DeleteProgram(_handle);
            _handle = 0;
        }
        GC.SuppressFinalize(this);
    }
}
