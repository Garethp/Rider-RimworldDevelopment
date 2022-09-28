## TODO List

 * General features:
   * Support LookupItems for Class="" attributes
   * Support LookupItems for things like <thoughtWorker> or <compClass>
   * If we know a bit of XML should be an enum like Gender, we should be able to autocomplete and highlight in red
   * Don't fail it there's no Rimworld Scope
   * Maybe bring out own Rimworld defs if there's no scope?
   * Handle Defs with custom classes instead of Rimworld classes
   * Packaging. It shouldn't be too difficult, but I haven't done it yet
   
 * Bugs
   * I got this bug on startup, worth investigating

```
Must be executed on UI thread or background threads with special permissions

java.lang.IllegalStateException: |E| Wrong thread null
	at com.jetbrains.rdclient.protocol.RdDispatcher.assertThread(RdDispatcher.kt:59)
	at com.jetbrains.rd.util.reactive.IScheduler$DefaultImpls.assertThread$default(Scheduler.kt:16)
	at com.jetbrains.rd.framework.base.RdReactiveBase.assertThreading(RdReactiveBase.kt:27)
	at com.jetbrains.rd.framework.base.RdReactiveBase.localChange$rd_framework(RdReactiveBase.kt:46)
	at com.jetbrains.rd.framework.impl.RdSet.add(RdSet.kt:81)
	at com.jetbrains.rd.util.collections.ModificationCookieViewableSet.add(ModificationCookieViewableSet.kt:10)
	at com.jetbrains.rd.framework.base.ExtWire$send$$inlined$lock$lambda$1.invoke(RdExtBase.kt:236)
	at com.jetbrains.rd.framework.base.ExtWire$send$$inlined$lock$lambda$1.invoke(RdExtBase.kt:172)
	at com.jetbrains.rd.framework.impl.ProtocolContexts.sendWithoutContexts$rd_framework(ProtocolContexts.kt:180)
	at com.jetbrains.rd.framework.base.ExtWire.send(RdExtBase.kt:232)
	at com.jetbrains.rd.framework.impl.RdCall.startInternal(RdTask.kt:254)
	at com.jetbrains.rd.framework.impl.RdCall.start(RdTask.kt:234)
	at com.jetbrains.rd.framework.impl.RdCall.start(RdTask.kt:231)
	at com.jetbrains.rdclient.quickDoc.FrontendEditorMouseHoverPopupManager$scheduleProcessing$1$1.run(FrontendEditorMouseHoverPopupManager.kt:62)
	at com.intellij.openapi.progress.impl.CoreProgressManager.registerIndicatorAndRun(CoreProgressManager.java:705)
	at com.intellij.openapi.progress.impl.CoreProgressManager.executeProcessUnderProgress(CoreProgressManager.java:647)
	at com.intellij.openapi.progress.impl.ProgressManagerImpl.executeProcessUnderProgress(ProgressManagerImpl.java:63)
	at com.jetbrains.rdclient.quickDoc.FrontendEditorMouseHoverPopupManager$scheduleProcessing$1.run(FrontendEditorMouseHoverPopupManager.kt:58)
	at com.intellij.util.concurrency.QueueProcessor.runSafely(QueueProcessor.java:240)
	at com.intellij.util.Alarm$Request.lambda$runSafely$0(Alarm.java:388)
	at com.intellij.codeWithMe.ClientId$Companion.withClientId(ClientId.kt:135)
	at com.intellij.codeWithMe.ClientId.withClientId(ClientId.kt)
	at com.intellij.util.Alarm$Request.runSafely(Alarm.java:388)
	at com.intellij.util.Alarm$Request.run(Alarm.java:377)
	at java.base/java.util.concurrent.Executors$RunnableAdapter.call(Executors.java:515)
	at java.base/java.util.concurrent.FutureTask.run(FutureTask.java:264)
	at com.intellij.util.concurrency.SchedulingWrapper$MyScheduledFutureTask.run(SchedulingWrapper.java:220)
	at com.intellij.util.concurrency.BoundedTaskExecutor.doRun(BoundedTaskExecutor.java:216)
	at com.intellij.util.concurrency.BoundedTaskExecutor.access$200(BoundedTaskExecutor.java:27)
	at com.intellij.util.concurrency.BoundedTaskExecutor$1.execute(BoundedTaskExecutor.java:195)
	at com.intellij.util.ConcurrencyUtil.runUnderThreadName(ConcurrencyUtil.java:213)
	at com.intellij.util.concurrency.BoundedTaskExecutor$1.run(BoundedTaskExecutor.java:184)
	at java.base/java.util.concurrent.ThreadPoolExecutor.runWorker(ThreadPoolExecutor.java:1128)
	at java.base/java.util.concurrent.ThreadPoolExecutor$Worker.run(ThreadPoolExecutor.java:628)
	at java.base/java.util.concurrent.Executors$PrivilegedThreadFactory$1$1.run(Executors.java:668)
	at java.base/java.util.concurrent.Executors$PrivilegedThreadFactory$1$1.run(Executors.java:665)
	at java.base/java.security.AccessController.doPrivileged(Native Method)
	at java.base/java.util.concurrent.Executors$PrivilegedThreadFactory$1.run(Executors.java:665)
	at java.base/java.lang.Thread.run(Thread.java:829)
```

 * \<li> handling
   * Can we look for, and try to handle, instances where it doesn't have a Type against it's List?
   * Reference **defNames**

 * Field list
   * Should we be checking that the field is public?

 * XML Autocomplete
   * Auto complete other XML Defs
   * Link to those other XML Defs
   
 * Refactoring
   * We're fetching symbol scopes a bit all over the place. Let's collect it into a SymbolScope helper class
   
 * Documentation
   * Add a **useful** README that shows how the different classes work together
   * Re-read and document References.RimworldXmlReference
   * Document the requirements for running the plugin in the first place (Having the Rimworld DLL as a C# Reference)
   * If you have an XML file open while Rider is still initializing, that file doesn't get autocompletion. Document that
   * Add some gifs showing this plugin in work
   * Add a Roadmap
   
 * Tests
   * It's not a serious plugin project without Tests IMO. Let's at least aim to get one or two unit tests to start with