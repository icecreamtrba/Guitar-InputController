package net.fabricmc.example;

import net.fabricmc.api.ClientModInitializer;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.option.KeyBinding;
import net.minecraft.entity.Entity;
import net.minecraft.entity.LivingEntity;
import net.minecraft.util.hit.EntityHitResult;
import net.minecraft.util.hit.HitResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.ServerSocket;
import java.net.Socket;

public class LocalCommandServerMod implements ClientModInitializer {
    private static final Logger LOGGER = LoggerFactory.getLogger("notes-controller-client");
    private MinecraftClient mc;

    @Override
    public void onInitializeClient() {
        mc = MinecraftClient.getInstance();
        LOGGER.info("[LocalCmdMod] Client mod loaded!");

        new Thread(() -> {
            try (ServerSocket serverSocket = new ServerSocket(5000)) {
                LOGGER.info("[LocalCmdMod] Listening on 127.0.0.1:5000");

                while (true) {
                    Socket client = serverSocket.accept();
                    BufferedReader reader = new BufferedReader(new InputStreamReader(client.getInputStream()));
                    String line;
                    while ((line = reader.readLine()) != null) {
                        LOGGER.info("[LocalCmdMod] Received: {}", line);
                        handleCommand(line.trim());
                    }
                }
            } catch (Exception e) {
                LOGGER.error("[LocalCmdMod] Server error", e);
            }
        }, "LocalCmdServerThread").start();
    }

    private void handleCommand(String cmd) {
        if (mc.player == null) return;

        // Основные привязки клавиш
        KeyBinding[] keys = new KeyBinding[] {
            mc.options.forwardKey,   // W
            mc.options.backKey,      // S
            mc.options.leftKey,      // A
            mc.options.rightKey,     // D
            mc.options.jumpKey,      // Space
            mc.options.attackKey,    // ЛКМ
            mc.options.useKey,       // ПКМ
            mc.options.sprintKey,    // Sprint
            mc.options.sneakKey,     // Sneak
            mc.options.inventoryKey  // Inventory
        };

        // Проверяем команду поворота отдельно
        if (cmd.toLowerCase().startsWith("set_rotation:")) {
            handleRotationCommand(cmd);
            return;
        }

        // ★★★★ ОБРАБОТКА КОМАНД С ВРЕМЕНЕМ УДЕРЖАНИЯ ★★★★
        if (cmd.toLowerCase().startsWith("forward:")) {
            handleTimedCommand(cmd, keys[0]);
            return;
        }
        if (cmd.toLowerCase().startsWith("back:")) {
            handleTimedCommand(cmd, keys[1]);
            return;
        }
        if (cmd.toLowerCase().startsWith("left:")) {
            handleTimedCommand(cmd, keys[2]);
            return;
        }
        if (cmd.toLowerCase().startsWith("right:")) {
            handleTimedCommand(cmd, keys[3]);
            return;
        }
        if (cmd.toLowerCase().startsWith("jump:")) {
            handleTimedCommand(cmd, keys[4]);
            return;
        }
        if (cmd.toLowerCase().startsWith("sprint:")) {
            handleTimedCommand(cmd, keys[7]);
            return;
        }
        if (cmd.toLowerCase().startsWith("sneak:")) {
            handleTimedCommand(cmd, keys[8]);
            return;
        }

        // ОБРАБОТКА ПЛАВНЫХ ПОВОРОТОВ С ПАРАМЕТРАМИ
        if (cmd.toLowerCase().startsWith("look_right:")) {
            handleSmoothRotationCommand(cmd, 15, 0);
            return;
        }
        if (cmd.toLowerCase().startsWith("look_left:")) {
            handleSmoothRotationCommand(cmd, -15, 0);
            return;
        }
        if (cmd.toLowerCase().startsWith("look_up:")) {
            handleSmoothRotationCommand(cmd, 0, -15);
            return;
        }
        if (cmd.toLowerCase().startsWith("look_down:")) {
            handleSmoothRotationCommand(cmd, 0, 15);
            return;
        }

        switch (cmd.toLowerCase()) {
            // ★★★★ КОРОТКИЕ КОМАНДЫ ДВИЖЕНИЯ (100ms по умолчанию) ★★★★
            case "forward": 
                handleTimedKey(keys[0], 100);
                break;
            case "back":    
                handleTimedKey(keys[1], 100);
                break;
            case "left":    
                handleTimedKey(keys[2], 100);
                break;
            case "right":   
                handleTimedKey(keys[3], 100);
                break;
            case "jump":    
                handleTimedKey(keys[4], 100);
                break;
            case "sprint": 
                handleTimedKey(keys[7], 100);
                break;
            case "sneak":  
                handleTimedKey(keys[8], 100);
                break;

            // ★★★★ РАЗДЕЛЕННЫЕ ДЕЙСТВИЯ ★★★★
            case "attack": // Атака по существам
                handleAttack();
                break;

            case "mine": // Копание блоков
                performClick(keys[5], 300); // Длинное нажатие для копания
                LOGGER.debug("[LocalCmdMod] Mining action");
                break;

            case "use": // Использование предметов
                performClick(keys[6], 150);
                break;

            case "use_hold": // Длительное использование (для лука, еды и т.д.)
                keys[6].setPressed(true);
                LOGGER.debug("[LocalCmdMod] Use hold");
                break;

            case "use_release": // Отпускание использования
                keys[6].setPressed(false);
                LOGGER.debug("[LocalCmdMod] Use release");
                break;

            // Инвентарь
            case "inventory":
                if (mc.player != null) {
                    mc.execute(() -> mc.setScreen(new net.minecraft.client.gui.screen.ingame.InventoryScreen(mc.player)));
                }
                break;
            case "esc":
                mc.execute(() -> mc.setScreen(null)); // Закрывает любой открытый GUI
                break;
                
            // Смена слота
            case "slot1": mc.player.getInventory().selectedSlot = 0; break;
            case "slot2": mc.player.getInventory().selectedSlot = 1; break;
            case "slot3": mc.player.getInventory().selectedSlot = 2; break;
            case "slot4": mc.player.getInventory().selectedSlot = 3; break;
            case "slot5": mc.player.getInventory().selectedSlot = 4; break;
            case "slot6": mc.player.getInventory().selectedSlot = 5; break;
            case "slot7": mc.player.getInventory().selectedSlot = 6; break;
            case "slot8": mc.player.getInventory().selectedSlot = 7; break;
            case "slot9": mc.player.getInventory().selectedSlot = 8; break;

            // Дроп
            case "drop": 
                mc.player.dropSelectedItem(false); 
                break;
            case "dropstack": 
                mc.player.dropSelectedItem(true); 
                break;

            // ★★★★ ОБЫЧНЫЕ ПОВОРОТЫ (для обратной совместимости) ★★★★
            case "look_up":
                smoothRotateCamera(0, -15, 10); // 10 шагов
                break;
            case "look_down":
                smoothRotateCamera(0, 15, 10);
                break;
            case "look_left":
                smoothRotateCamera(-15, 0, 10);
                break;
            case "look_right":
                smoothRotateCamera(15, 0, 10);
                break;

            // ★★★★ МЕЛКИЕ ПОВОРОТЫ ★★★★
            case "look_up_small":
                smoothRotateCamera(0, -5, 5);
                break;
            case "look_down_small":
                smoothRotateCamera(0, 5, 5);
                break;
            case "look_left_small":
                smoothRotateCamera(-5, 0, 5);
                break;
            case "look_right_small":
                smoothRotateCamera(5, 0, 5);
                break;

            default:
                LOGGER.warn("[LocalCmdMod] Unknown command: {}", cmd);
                break;
        }
    }

    // ★★★★ МЕТОД ДЛЯ ОБРАБОТКИ КОМАНД С ВРЕМЕНЕМ УДЕРЖАНИЯ ★★★★
    private void handleTimedCommand(String cmd, KeyBinding key) {
        try {
            // Формат: "forward:200" где 200 - время удержания в ms
            String[] parts = cmd.split(":");
            if (parts.length == 2) {
                int holdTime = Integer.parseInt(parts[1]);
                handleTimedKey(key, holdTime);
                LOGGER.info("[LocalCmdMod] Timed command: {} for {}ms", parts[0], holdTime);
            }
        } catch (NumberFormatException e) {
            LOGGER.error("[LocalCmdMod] Invalid hold time: {}", cmd);
        }
    }

    // ★★★★ МЕТОД ДЛЯ КЛАВИШ С ВРЕМЕНЕМ УДЕРЖАНИЯ ★★★★
    private void handleTimedKey(KeyBinding key, int holdTimeMs) {
        if (mc.player == null) return;
        
        mc.execute(() -> {
            // Нажимаем клавишу
            key.setPressed(true);
            
            // Отпускаем через указанное время
            new Thread(() -> {
                try {
                    Thread.sleep(holdTimeMs);
                    mc.execute(() -> {
                        key.setPressed(false);
                        LOGGER.debug("[LocalCmdMod] Released key after {}ms", holdTimeMs);
                    });
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                }
            }).start();
        });
    }

    // ★★★★ МЕТОД ДЛЯ АТАКИ СУЩЕСТВ ★★★★
    private void handleAttack() {
        if (mc.player == null) return;
        
        mc.execute(() -> {
            // Сначала проверяем есть ли цель для атаки
            if (mc.crosshairTarget != null && mc.crosshairTarget.getType() == HitResult.Type.ENTITY) {
                try {
                    Entity target = ((EntityHitResult) mc.crosshairTarget).getEntity();
                    
                    // Атакуем только живые существа
                    if (target instanceof LivingEntity) {
                        mc.interactionManager.attackEntity(mc.player, target);
                        LOGGER.info("[LocalCmdMod] Attacking entity: {}", target.getName().getString());
                        return; // Выходим если атаковали существо
                    }
                } catch (Exception e) {
                    LOGGER.warn("[LocalCmdMod] Entity attack failed: {}", e.getMessage());
                }
            }
            
            // Если нет подходящей цели или атака не удалась - обычное нажатие ЛКМ
            performClick(mc.options.attackKey, 50);
        });
    }

    // ★★★★ МЕТОД ДЛЯ УПРАВЛЕНИЯ НАЖАТИЯМИ КЛАВИШ ★★★★
    private void performClick(KeyBinding key, int holdTimeMs) {
        if (mc.player == null) return;
        
        mc.execute(() -> {
            // Нажимаем клавишу
            key.setPressed(true);
            
            // Отпускаем через указанное время
            new Thread(() -> {
                try {
                    Thread.sleep(holdTimeMs);
                    mc.execute(() -> {
                        key.setPressed(false);
                    });
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                }
            }).start();
        });
    }

    // ★★★★ МЕТОД ДЛЯ ОБРАБОТКИ ПЛАВНЫХ ПОВОРОТОВ С ПАРАМЕТРАМИ ★★★★
    private void handleSmoothRotationCommand(String cmd, float yawChange, float pitchChange) {
        try {
            // Формат: "look_right:10" или "look_up:5"
            String[] parts = cmd.split(":");
            if (parts.length == 2) {
                int steps = Integer.parseInt(parts[1]);
                smoothRotateCamera(yawChange, pitchChange, steps);
                LOGGER.info("[LocalCmdMod] Smooth rotation: {} steps", steps);
            }
        } catch (NumberFormatException e) {
            LOGGER.error("[LocalCmdMod] Invalid steps value: {}", cmd);
        }
    }

    // ★★★★ МЕТОД ДЛЯ ТОЧНОЙ УСТАНОВКИ УГЛОВ ★★★★
    private void handleRotationCommand(String cmd) {
        try {
            // Формат: "set_rotation:yaw:pitch"
            String[] parts = cmd.split(":");
            if (parts.length == 3) {
                float yaw = Float.parseFloat(parts[1]);
                float pitch = Float.parseFloat(parts[2]);
                
                // Ограничиваем pitch
                pitch = Math.max(-90.0f, Math.min(90.0f, pitch));
                
                // Нормализуем yaw
                yaw %= 360.0f;
                if (yaw < 0) yaw += 360.0f;
                
                // Устанавливаем углы
                mc.player.setYaw(yaw);
                mc.player.setPitch(pitch);
                
                LOGGER.info("[LocalCmdMod] Exact rotation set: yaw={}, pitch={}", yaw, pitch);
            }
        } catch (NumberFormatException e) {
            LOGGER.error("[LocalCmdMod] Invalid rotation values: {}", cmd);
        }
    }

    // ★★★★ МЕТОД ДЛЯ СУПЕР-ПЛАВНОГО ПОВОРОТА ★★★★
    private void smoothRotateCamera(float yawChange, float pitchChange, int steps) {
        if (mc.player == null) return;
        
        new Thread(() -> {
            try {
                float yawStep = yawChange / steps;
                float pitchStep = pitchChange / steps;
                
                for (int i = 0; i < steps; i++) {
                    if (mc.player == null) break;
                    
                    // Выполняем в основном потоке Minecraft
                    mc.execute(() -> {
                        float currentYaw = mc.player.getYaw();
                        float currentPitch = mc.player.getPitch();
                        
                        float newYaw = currentYaw + yawStep;
                        float newPitch = currentPitch + pitchStep;
                        
                        // Ограничиваем pitch
                        newPitch = Math.max(-90.0f, Math.min(90.0f, newPitch));
                        
                        // Нормализуем yaw
                        newYaw %= 360.0f;
                        if (newYaw < 0) newYaw += 360.0f;
                        
                        mc.player.setYaw(newYaw);
                        mc.player.setPitch(newPitch);
                    });
                    
                    Thread.sleep(20); // 20ms между шагами = 50 FPS
                }
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
            }
        }).start();
    }
}