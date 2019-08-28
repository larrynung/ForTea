package com.jetbrains.fortea.configuration.run.task

import com.intellij.execution.BeforeRunTaskProvider
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.util.Key
import com.intellij.util.concurrency.Semaphore
import com.jetbrains.fortea.configuration.T4BuildSessionView
import com.jetbrains.fortea.configuration.run.T4RunConfiguration
import com.jetbrains.rider.model.T4BuildResultKind
import com.jetbrains.rider.model.t4ProtocolModel
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.getComponent
import com.jetbrains.rider.util.idea.lifetime

class CompileT4BeforeRunTaskProvider : BeforeRunTaskProvider<CompileT4BeforeRunTask>() {
  override fun getName() = "Compile T4 file"

  override fun getId(): Key<CompileT4BeforeRunTask> = providerId

  override fun createTask(runConfiguration: RunConfiguration): CompileT4BeforeRunTask? {
    if (runConfiguration !is T4RunConfiguration) return null
    val task = CompileT4BeforeRunTask()
    task.isEnabled = true
    return task
  }

  override fun executeTask(
    context: DataContext,
    configuration: RunConfiguration,
    env: ExecutionEnvironment,
    task: CompileT4BeforeRunTask
  ): Boolean {
    if (configuration !is T4RunConfiguration) return false

    val project = configuration.project
    val view = project.getComponent<T4BuildSessionView>()
    view.openWindow(project.lifetime)
    val finished = Semaphore()
    finished.down()
    var successful = false

    val host = ProjectModelViewHost.getInstance(project)
    val initialFileLocation = configuration.parameters.initialFileLocation
    val item = host.getItemById(initialFileLocation.id) ?: return false
    val path = item.getVirtualFile()?.path ?: return false
    val model = project.solution.t4ProtocolModel

    val request = model.requestCompilation.start(initialFileLocation).result
    request.advise(project.lifetime) { rdTaskResult ->
      try {
        val result = rdTaskResult.unwrap()
        successful = result.buildResultKind.isSuccess
        view.showT4BuildResult(project.lifetime, result.messages, path)
      } finally {
        finished.up()
      }
    }

    finished.waitFor()
    if (!successful) model.executionAborted.fire(initialFileLocation)
    return successful
  }

  companion object {
    val providerId = Key.create<CompileT4BeforeRunTask>("Compile T4")

    private val T4BuildResultKind.isSuccess
      get() = when (this) {
        T4BuildResultKind.HasErrors -> false
        T4BuildResultKind.HasWarnings -> true
        T4BuildResultKind.Successful -> true
      }
  }
}
