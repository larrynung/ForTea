package model

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rider.model.nova.ide.SolutionModel

@Suppress("unused")
object T4ProtocolModel : Ext(SolutionModel.Solution) {
  val T4BuildMessageKind = enum {
    +"Message"
    +"Success"
    +"Warning"
    +"Error"
  }

  val T4Location = structdef {
    field("line", int)
    field("column", int)
  }

  val T4BuildMessage = structdef {
    field("buildMessageKind", T4BuildMessageKind)
    field("id", string)
    field("location", T4Location)
    field("content", string)
    field("projectId", int)
  }

  val T4BuildResultKind = enum {
    +"HasErrors"
    +"HasWarnings"
    +"Successful"
  }

  val T4BuildResult = structdef {
    field("buildResultKind", T4BuildResultKind)
    field("messages", immutableList(T4BuildMessage))
  }

  val T4ConfigurationModel = structdef {
    field("executablePath", string)
    field("outputPath", string)
  }

  init {
    map("configurations", string, T4ConfigurationModel).async
    call("requestCompilation", string, T4BuildResult).async
    call("transferResults", string, void).async
    call("requestPreprocessing", string, bool)
  }
}
